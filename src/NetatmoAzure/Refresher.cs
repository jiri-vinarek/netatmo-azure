using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;

namespace NetatmoAzure
{
    public static class Refresher
    {
        private static readonly Uri AuthUri = new Uri("https://api.netatmo.com/oauth2/token");

        [FunctionName("Refresher")]
        public static void Run([TimerTrigger("0 0 */1 * * *", RunOnStartup = true)]TimerInfo timer, TraceWriter log, ExecutionContext context)
        {
            log.Info($"Refresher executed at: {DateTime.Now}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var requestPayload = new Dictionary<string, string>()
            {
                {"grant_type", "password"},
                {"username", config["NetatmoUsername"]},
                {"password", config["NetatmoPassword"]},
                {"client_id", config["NetatmoClientId"]},
                {"client_secret", config["NetatmoClientSecret"]},
                {"scope", "read_homecoach"}
            };

            var response = new HttpClient().PostAsync(AuthUri, new FormUrlEncodedContent(requestPayload)).Result;

            var result = response.Content.ReadAsStringAsync().Result;
            log.Info(result);
        }
    }
}
