using Azure;
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Azure.AI.TextAnalytics;
using System.Threading.Tasks;
using MyAnalyticsLibrary;
using System.Text.Json;
using System.Linq;
using System.Text.Json;

namespace TranslatorFunction
{
    public static class TranslatorFunc
    {
        private static readonly AzureKeyCredential credentials =
            new AzureKeyCredential("009878e0c89740028a5e989f964c7ca5"); //TODO: Hide in locals

        private static readonly Uri endpoint =
            new Uri("https://language-cognitive651654654.cognitiveservices.azure.com/");


        [FunctionName("TranslatorFunc")]
        public static async Task Run(
            [QueueTrigger("myqueue-messages", Connection = "AzureWebJobsStorage")] string triggerMessage,
            [Queue("result-messages"), StorageAccount("AzureWebJobsStorage")] ICollector<string> resultQue,
            ILogger logger)
        {
            logger.LogInformation($"Info: queue trigger function: {triggerMessage}");

            var analyticsClient = new TextAnalyticsClient(endpoint, credentials);

            //var entry = JsonSerializer.Deserialize<AnalyticsEntry>(triggerMessage);

            try
            {
                var keyTask = await testKeyPhrase(analyticsClient, entry, logger);
                resultQue.Add(JsonSerializer.Serialize(keyTask));
            }
            catch (Exception e)
            {
                logger.LogInformation($"Error: {e}");
            }
        }

        static async Task<AnalyticsEntryProcessed> testKeyPhrase(TextAnalyticsClient _client, 
            AnalyticsEntry entry, ILogger _logger)
        {
            string composedPostString = "";
            foreach (string iPost in entry.RawPosts)
            {
                composedPostString += iPost;
                if (composedPostString.Length > 0 &&
                    composedPostString[composedPostString.Length - 1] != '.' |
                    composedPostString[composedPostString.Length - 1] != '!' |
                    composedPostString[composedPostString.Length - 1] != '?')
                    composedPostString += '.';
            }
            Response<KeyPhraseCollection> response = await _client.ExtractKeyPhrasesAsync(composedPostString);

            KeyPhraseCollection keyPhrases = response.Value;

            return new AnalyticsEntryProcessed()
            {
                Subreddit = entry.Subreddit,
                RawPosts = entry.RawPosts,
                ProcessedPosts = keyPhrases.ToArray()
            };
        }
    }
}