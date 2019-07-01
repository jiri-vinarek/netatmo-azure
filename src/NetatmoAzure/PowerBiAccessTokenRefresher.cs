using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NetatmoAzure
{
    public static class PowerBiAccessTokenRefresher
    {
        public static readonly string PowerBiAccessTokenBlobStorageContainerName = "powerbiaccesstokencontainer";
        public static readonly string PowerBiAccessTokenBlobStorageBlobName = "powerbiaccesstoken";

        [FunctionName("PowerBiAccessTokenRefresher")]
        //public static void Run([TimerTrigger("0 0/50 * * * *", RunOnStartup = true)]TimerInfo timer, TraceWriter log, ExecutionContext context)
        public static void Run([TimerTrigger("0 */1 * * * *", RunOnStartup = true)]TimerInfo timer, TraceWriter log, ExecutionContext context)
        {
            log.Info($"PowerBiAccessTokenRefresher executed at: {DateTime.Now}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var azureConnectionString = config.GetConnectionString("Azure");

            var storageAccount = CloudStorageAccount.Parse(azureConnectionString);
            var client = storageAccount.CreateCloudBlobClient();

            // TODO - get refresh token from config
            // TODO - get access token
            

            var container = client.GetContainerReference(PowerBiAccessTokenBlobStorageContainerName);
            container.CreateIfNotExists();

            // TODO - save access token into file
            var blob = container.GetBlockBlobReference(PowerBiAccessTokenBlobStorageBlobName);
            //blob.UploadText("TEST123 " + DateTime.Now.ToString());
        }
    }
}
