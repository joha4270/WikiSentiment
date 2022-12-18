using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Configuration;
using static WikiScraper.WikiData;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using Azure.Data.Tables;
using System.Collections.Concurrent;
using Azure;
using System.Data.Common;

namespace WikiScraper
{
    public class Function1
    {
        private readonly IConfiguration config;
        IDBClient dbClient;
        HttpClient httpClient;
        private string[] countrycodes;// = {
                                      //"en", "pl", "ar", "de", "es", "fr", "it", "nl", "ja", "pt", "sv", "uk", "vi", "zh", "ru" };
        private Dictionary<string, List<string>> countryExceptions;

        public Function1(IConfiguration iConfig)
        {
            config = iConfig;
            countryExceptions = buildConfigExceptions();
            countrycodes = countryExceptions.Keys.ToArray();
            dbClient = getGitClient(config);
            httpClient = getHttpClient(config);
        }


        [FunctionName("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("sentimenttable"), StorageAccount("AzureWebJobsStorage")] TableClient client, 
            ILogger log)
        {
            bool uploadData = true;    
            log.LogInformation("C# HTTP trigger function processed a request.");

            bool forceRebuild = false;
            var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day - 2);
            date = date.AddDays(-30);
            int daysToGo = 1;

            MonthlyCollection monthlyCollection = null;
            while (daysToGo > 0)
            {
                string dayString = $"{date.Year}{date.Month:D2}{date.Day:D2}";
                string monthString = $"{date.Year}{date.Month:D2}";

                //if month changed, make a new collection
                if (monthlyCollection == null || monthlyCollection.date != monthString)
                {
                    //upload previous collection
                    if (uploadData && monthlyCollection != null)
                    {
                        await dbClient.Upload(monthString, monthlyCollection.ToJSON());
                    }

                    monthlyCollection = new MonthlyCollection(monthString);

                    //if not rebuilding database, try loading a new collection from db
                    if (!forceRebuild)
                    {
                        var dbRequest = await dbClient.Load(monthString);
                        if (dbRequest.result == System.Net.HttpStatusCode.OK)
                            monthlyCollection = JsonSerializer.Deserialize<MonthlyCollection>(dbRequest.content);
                    }
                }

                if (!monthlyCollection.dailyData.ContainsKey(dayString))
                {
                    monthlyCollection.dailyData[dayString] = 
                        await DailyCollection.BuildCollection(date, countrycodes, countryExceptions, httpClient);

                }

                daysToGo -= 1;
                date.AddDays(-1);
            }
            
            if (uploadData)
                await dbClient.Upload(monthlyCollection.date, monthlyCollection.ToJSON());
            //await client.UpsertEntityAsync(tableEntity);

            //var getting = await client.GetEntityAsync<TableEntity>(val.PartitionKey, val.RowKey);
            //var test = System.Text.Json.JsonSerializer.Deserialize<DailyCollection>(getting.Value.GetString("Collection"));

            return new OkObjectResult("all done ^_^");
        }

        HttpClient getHttpClient(IConfiguration _config)
        {
            var result = new HttpClient();

            //result.BaseAddress = new Uri(_config.GetValue<string>("FunctionValues:WikiBaseUri"));

            result.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                 _config.GetValue<string>("FunctionValues:WikiAPIToken"));

            result.DefaultRequestHeaders.Add("Api-User-Agent",
                _config.GetValue<string>("FunctionValues:WikiUserContact"));

            return result;
        }

        GithubUploader getGitClient(IConfiguration _config)
        {
            return new GithubUploader(
                        _config.GetValue<string>("FunctionValues:GitHub:ProductHeader"),
                        _config.GetValue<string>("FunctionValues:GitHub:Token"),
                        _config.GetValue<string>("FunctionValues:GitHub:Owner"),
                        _config.GetValue<string>("FunctionValues:GitHub:Repo"),
                        _config.GetValue<string>("FunctionValues:GitHub:Branch"),
                        _config.GetValue<string>("FunctionValues:GitHub:FilePath"));
        }

        /// <summary>
        /// Deserializes country codes and exception lists for them from app settings
        /// </summary>
        /// <returns></returns>
        Dictionary<string, List<string>> buildConfigExceptions()
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            //splice a section of the config for itemization into a list
            var configValue = config.GetSection("FunctionValues:CountryCodes");
            
            foreach (var configObject in configValue.GetChildren())
            {

                result[configObject.Value] = new List<string>();

                //splice a section from exceptions into a list
                var excepValues = config.GetSection($"FunctionValues:CountryExceptions:{configObject.Value}");

                foreach (var exceptObject in excepValues.GetChildren())
                {
                    result[configObject.Value].Add(exceptObject.Value);
                }
            }

            return result;
        }
    }
}
