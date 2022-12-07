using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using MyAnalyticsLibrary;
using System.Text.Json.Nodes;


namespace ScraperFunction
{
    public static class ScraperFunc
    {

        [FunctionName("ScraperFunc")]
        public static async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer,
            [Queue("myqueue-messages"), StorageAccount("AzureTranslatorQueue")] ICollector<string> msg, 
            ILogger log)
        {
            log.LogInformation($"Info: Timer trigger function executed at: {DateTime.Now}");
            
            var scraper = new RedditJ("https://www.reddit.com/r/");

            string[] subredditList = { "games", "news" };
            List<Task<AnalyticsEntry>> entryTasks = new List<Task<AnalyticsEntry>>();
            foreach (string subreddit in subredditList)
            {
                
                try {
                    entryTasks.Add(scraper.GetEntry(subreddit));
                }
                catch (Exception e) { log.LogInformation($"Error: {e}"); }
                
            }

            foreach (Task<AnalyticsEntry> task in entryTasks)
            {
                var entry = await task;
                msg.Add(JsonSerializer.Serialize(entry));
            }

            //Dictionary<string, List<string>> postnames = await scraper.GetWebData(subredditList);

            //string logstring = "";

            //foreach (string iKey in postnames.Keys)
            //{
            //logstring += "/r/" + iKey + ":" + '\n';

            //foreach (string iTitle in postnames[iKey])
            //{
            //logstring += iTitle + '\n';
            //}

            //logstring += '\n';
            //}
            //log.LogInformation(logstring);
            //try
            //{
            //  msg.Add(logstring);
            //}
            //catch
            //{
            //log.LogInformation($"Error adding message to the queue {logstring}");
            //                
            //}
        }

        static String toBase64String(this String source)
        {
            return Convert.ToBase64String(Encoding.Unicode.GetBytes(source));
        }
        //[FunctionName("ScraperFunc")]
        //public static async Task<IActionResult> Run(
        //[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        //ILogger log)
        //{
        //log.LogInformation("C# HTTP trigger function processed a request.");
        //
        //string name = req.Query["name"];
        //
        //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //dynamic data = JsonConvert.DeserializeObject(requestBody);
        //name = name ?? data?.name;
        //
        //string responseMessage = string.IsNullOrEmpty(name)
        //? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
        //: $"Hello, {name}. This HTTP triggered function executed successfully.";
        //
        //return new OkObjectResult(responseMessage);
        //}
    }
}
