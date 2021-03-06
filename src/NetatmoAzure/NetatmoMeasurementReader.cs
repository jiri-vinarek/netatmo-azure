using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetatmoAzure
{
    public static class NetatmoMeasurementReader
    {
        private static readonly Uri HomeCoachsDataUri = new Uri("https://api.netatmo.com/api/gethomecoachsdata");

        private static readonly string PowerBiPushRowsUriString = "https://api.powerbi.com/v1.0/myorg/groups/{0}/datasets/{1}/tables/{2}/rows";

        [FunctionName("NetatmoMeasurementReader")]
        public static void Run([TimerTrigger("0 5-59/10 * * * *", RunOnStartup = true)]TimerInfo timer, TraceWriter log, ExecutionContext context)
        {
            log.Info($"ValueReader executed at: {DateTime.Now}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var requestPayload = new Dictionary<string, string>
            {
                {"access_token", config["NetatmoAccessToken"]},
                {"device_id", config["NetatmoDeviceId"]}
            };

            var response = new HttpClient().PostAsync(HomeCoachsDataUri, new FormUrlEncodedContent(requestPayload)).Result;

            var result = response.Content.ReadAsStringAsync().Result;
            log.Info(result);

            var azureConnectionString = config.GetConnectionString("Azure");
            var macAddressesWithMeasurements = Parse(result);

            // This block here serves as a mechanism to overcome multiple inserts of a same row into PowerBI push dataset.
            // The problem is the PowerBI push DB which sums values of the duplicate rows.
            // In that case SaveToPowerBi throws StorageException.
            try
            {
                SaveToTableStorage(macAddressesWithMeasurements, azureConnectionString);

                var powerBiAccessToken = ReadPowerBiAccessTokenFromBlobStorage(azureConnectionString);
                SaveToPowerBi(
                    macAddressesWithMeasurements: macAddressesWithMeasurements,
                    powerBiGroupId: config["PowerBiGroupId"],
                    powerBiDatasetId: config["PowerBiDatasetId"],
                    powerBiTable: config["PowerBiTable"],
                    accessToken: powerBiAccessToken,
                    log: log
                );
            }
            catch (StorageException e)
            {
                log.Error(e.ToString());
            }
        }

        private static IEnumerable<MacAddressWithMeasurement> Parse(string json)
        {
            var jObject = JObject.Parse(json);
            return jObject["body"]["devices"].Children().Select(device => {
                var macAddress = device["_id"].Value<string>();
                var measurement = device["dashboard_data"].ToObject<Measurement>();
                return new MacAddressWithMeasurement(macAddress, measurement);
            });
        }

        private static void SaveToTableStorage(IEnumerable<MacAddressWithMeasurement> macAddressesWithMeasurements, string azureTableStorageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(azureTableStorageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("measurements");
            table.CreateIfNotExists();

            foreach (var m in macAddressesWithMeasurements)
            {
                var insertOperation = TableOperation.Insert(new MeasurementTableEntity(m.MacAddress, m.Measurement));
                var result = table.Execute(insertOperation);
            }
        }

        private static string ReadPowerBiAccessTokenFromBlobStorage(string azureConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(azureConnectionString);
            var client = storageAccount.CreateCloudBlobClient();

            var container = client.GetContainerReference(PowerBiAccessTokenRefresher.PowerBiAccessTokenBlobStorageContainerName);
            var blob = container.GetBlockBlobReference(PowerBiAccessTokenRefresher.PowerBiAccessTokenBlobStorageBlobName);

            return blob.DownloadText();
        }

        private static void SaveToPowerBi(IEnumerable<MacAddressWithMeasurement> macAddressesWithMeasurements, string powerBiGroupId, string powerBiDatasetId, string powerBiTable, string accessToken, TraceWriter log)
        {
            var pushRowsUri = new Uri(string.Format(PowerBiPushRowsUriString, powerBiGroupId, powerBiDatasetId, powerBiTable));

            var rowsPowerBi = new RowsPowerBi() { rows = macAddressesWithMeasurements.Select(ConvertMeasurement).ToList() };
            var rowsPowerBiSerialized = JsonConvert.SerializeObject(rowsPowerBi);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new StringContent(rowsPowerBiSerialized, Encoding.UTF8, "application/json");
            var response = client.PostAsync(pushRowsUri, content).Result;

            log.Info(response.ToString());
        }

        private static MeasurementPowerBi ConvertMeasurement(MacAddressWithMeasurement macAddressWithMeasurement)
        {
            var dateTimeUtc = DateTimeOffset.FromUnixTimeSeconds(macAddressWithMeasurement.Measurement.Time_utc).DateTime;
            TimeZoneInfo pragueTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
            var dateTimePrague = TimeZoneInfo.ConvertTimeFromUtc(dateTimeUtc, pragueTimeZone);

            var measurementPowerBi = new MeasurementPowerBi
            {
                CO2 = macAddressWithMeasurement.Measurement.CO2,
                Humidity = macAddressWithMeasurement.Measurement.Humidity,
                Noise = macAddressWithMeasurement.Measurement.Noise,
                Pressure = macAddressWithMeasurement.Measurement.Pressure,
                Temperature = macAddressWithMeasurement.Measurement.Temperature,
                MacAddress = macAddressWithMeasurement.MacAddress,
                DateTimeUtc = dateTimeUtc,
                DateTimePrague = dateTimePrague
            };

            return measurementPowerBi;
        }
    }
}
