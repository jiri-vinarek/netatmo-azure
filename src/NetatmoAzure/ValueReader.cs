using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;

namespace NetatmoAzure
{
    public static class ValueReader
    {
        private static readonly Uri HomeCoachsDataUri = new Uri("https://api.netatmo.com/api/gethomecoachsdata");

        [FunctionName("ValueReader")]
        public static void Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]TimerInfo timer, TraceWriter log, ExecutionContext context)
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

            var macAddressesWithMeasurements = Parse(result);
            var azureTableStorageConnectionString = config.GetConnectionString("AzureTableStorage");
            Save(macAddressesWithMeasurements, azureTableStorageConnectionString);
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

        private static void Save(IEnumerable<MacAddressWithMeasurement> macAddressesWithMeasurements, string azureTableStorageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(azureTableStorageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("measurements");
            table.CreateIfNotExists();

            foreach (var m in macAddressesWithMeasurements)
            {
                var insertOperation = TableOperation.InsertOrReplace(new MeasurementTableEntity(m.MacAddress, m.Measurement));
                table.Execute(insertOperation);
            }
        }
    }
}
