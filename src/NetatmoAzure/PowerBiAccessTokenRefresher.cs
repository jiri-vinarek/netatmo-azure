using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;

namespace NetatmoAzure
{
    public static class PowerBiAccessTokenRefresher
    {
        public static readonly string PowerBiAccessTokenBlobStorageContainerName = "powerbiaccesstokencontainer";
        public static readonly string PowerBiAccessTokenBlobStorageBlobName = "powerbiaccesstoken";

        private static readonly string PowerBiScope = "https://analysis.windows.net/powerbi/api/dataflow.readwrite.all";
        private static readonly Uri PowerBiOauthUri = new Uri("https://login.microsoftonline.com/organizations/oauth2/v2.0/token");

        [FunctionName("PowerBiAccessTokenRefresher")]
        public static void Run([TimerTrigger("0 */30 * * * *", RunOnStartup = true)]TimerInfo timer, TraceWriter log, ExecutionContext context)
        {
            log.Info($"PowerBiAccessTokenRefresher executed at: {DateTime.Now}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            
            var requestPayload = new Dictionary<string, string>
            {
                {"grant_type", "password"},
                {"client_id", config["PowerBiClientId"]},
                {"client_secret", config["PowerBiClientSecret"]},
                {"username", config["PowerBiUsername"]},
                {"password", config["PowerBiPassword"]},
                {"scope", PowerBiScope},
            };

            var response = new HttpClient().PostAsync(PowerBiOauthUri, new FormUrlEncodedContent(requestPayload)).Result;

            var result = response.Content.ReadAsStringAsync().Result;
            log.Info(result);

            var accessToken = ParseAccessToken(result);

            var azureConnectionString = config.GetConnectionString("Azure");
            SaveToBlobStorage(accessToken, azureConnectionString);
        }

        private static string ParseAccessToken(string json)
        {
            var jObject = JObject.Parse(json);
            return jObject["access_token"].Value<string>();
        }

        private static void SaveToBlobStorage(string accessToken, string azureConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(azureConnectionString);
            var client = storageAccount.CreateCloudBlobClient();

            var container = client.GetContainerReference(PowerBiAccessTokenBlobStorageContainerName);
            container.CreateIfNotExists();
            
            var blob = container.GetBlockBlobReference(PowerBiAccessTokenBlobStorageBlobName);
            blob.UploadText(accessToken);
        }
    }
}
