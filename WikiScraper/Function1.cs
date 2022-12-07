using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Extensions.Configuration;
using static WikiScraper.WikiData;
using System.Diagnostics.Metrics;
using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using Azure.Data.Tables;
using System.Collections.Concurrent;
using Azure;

namespace WikiScraper
{
    public class Function1
    {
        private readonly IConfiguration config;
        private string[] countrycodes;// = {
                                      //"en", "pl", "ar", "de", "es", "fr", "it", "nl", "ja", "pt", "sv", "uk", "vi", "zh", "ru" };
        private Dictionary<string, List<string>> countryExceptions;

        public Function1(IConfiguration iConfig)
        {
            config = iConfig;
            countryExceptions = getRegionsAndExceptions();
            countrycodes = countryExceptions.Keys.ToArray();
        }

        [FunctionName("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("sentimenttable"), StorageAccount("AzureWebJobsStorage")] TableClient client, 
            ILogger log)
        {

            
            log.LogInformation("C# HTTP trigger function processed a request.");

            //Initiate http getter, pass it base uri from config file

            //WikiJSON.RootJSON wr = await wiki.GetEntry(country, year, month, day);

            var date = DateTime.Today.AddDays(-1);
           
            var collection = await DailyCollection.BuildCollection(date, countrycodes, countryExceptions, getHttpClient(config));
            var tableEntity = new TableEntity()
            {
                PartitionKey = "main",
                RowKey = $"{ date.Year }{date.Month:D2}{date.Day:D2}"
            };
            tableEntity.Add("Collection", collection.ToJSON());

            await client.UpsertEntityAsync(tableEntity);
            //var getting = await client.GetEntityAsync<TableEntity>(val.PartitionKey, val.RowKey);
            //var test = System.Text.Json.JsonSerializer.Deserialize<DailyCollection>(getting.Value.GetString("Collection"));

            return new OkObjectResult(collection.ToString());
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

        /// <summary>
        /// Deserializes country codes and exception lists for them from app settings
        /// </summary>
        /// <returns></returns>
        Dictionary<string, List<string>> getRegionsAndExceptions()
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
