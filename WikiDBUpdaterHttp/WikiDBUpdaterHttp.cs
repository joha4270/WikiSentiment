using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using Azure.Data.Tables;
using WikiSentiment;


namespace WikiDBUpdaterHttp
{
    /// <summary>
    /// HTTP triggered function that updates DB with new wiki collections
    /// </summary>
    public class WikiDBUpdaterHttp
    {
        private ConfigurationWrapper config;

        private string[] allLanguageCodes;

        private HttpClient httpClient;

        private Dictionary<string, string[]> articleExceptions;

        public WikiDBUpdaterHttp(IConfiguration iConfig)
        {
            config = new ConfigurationWrapper(iConfig);

            articleExceptions = config.GetValue<Dictionary<string, string[]>>
                ("FunctionValues:CountryExceptions");

            allLanguageCodes = config.GetValue<string[]>("FunctionValues:CountryCodes");


            httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                 config.GetValue<string>("WikiKeys:WikiAPIToken"));

            httpClient.DefaultRequestHeaders.Add("Api-User-Agent",
                config.GetValue<string>("WikiKeys:WikiUserContact"));
        }

        /// <summary>
        /// Azure binding that launches the function
        /// </summary>
        /// <param name="req"></param>
        /// <param name="tableClient"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("WikiDBUpdaterHttp")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("datatables"), StorageAccount("AzureWebJobsStorage")] TableClient tableClient, 
            ILogger log)
        {
            //url schema is /WikiDBUpdaterHttp?date=2021-12-31&days=3&discard&language=de,fr
            //get and validate options
            string YYYYMMDD = req.Query["date"];
            DateTime startDate;
            if (YYYYMMDD == null)
                return new BadRequestObjectResult("Missing parameter: date");
            if (!validateDate(YYYYMMDD, out startDate))
                return new BadRequestObjectResult("Bad starting date: " + YYYYMMDD);

            int daysToGo = 1;
            string daysToGoString = req.Query["days"];
            if (daysToGoString != null)
                if (!int.TryParse(daysToGoString, out daysToGo))
                    return new BadRequestObjectResult("Bad days parameter: " + daysToGoString);


            string[] languages = allLanguageCodes;
            string languageStrings = req.Query["languages"];
            if (languageStrings != null)
                if (!validateLanguages(languageStrings, out languages))
                    return new BadRequestObjectResult("Bad languages parameter: " + languageStrings);


            string discardString = req.Query["discard"];
            bool discardOldData = discardString != null ? true : false;

            IDBClient dbClient = new AzureStorageClient(tableClient);

            await DBUpdates.updateDatabase(startDate, daysToGo, discardOldData, languages, 
                articleExceptions, httpClient, dbClient, log);
            
            return new OkObjectResult("Successfull execution");
        }

        /// <summary>
        /// Parses the YYYY-MM-DD string into DateTime
        /// </summary>
        /// <param name="_yyyymmdd">Date in a YYYY-MM-DD format</param>
        /// <param name="_date">out for a date</param>
        /// <returns>true if date was correctly parsed</returns>
        bool validateDate(string _yyyymmdd, out DateTime _date)
        {
            _date = DateTime.Now;
            int year, month, day;
            string[] dateArray = _yyyymmdd.Split('-');

            if (dateArray.Length != 3)
                return false;

            if (!int.TryParse(dateArray[0], out year))
                return false;
            if (!int.TryParse(dateArray[1], out month))
                return false;
            if (!int.TryParse(dateArray[2], out day))
                return false;

            _date = new DateTime(year, month, day);
            return true;
        }

       
        /// <summary>
        /// Validates list of languages in "","","" format
        /// </summary>
        /// <param name="_language">string in "","","", format</param>
        /// <param name="_languageArray">out for an output array</param>
        /// <returns>returns true if string parsed into array</returns>
        bool validateLanguages(string _language, out string[] _languageArray)
        {
            _languageArray = _language.ToLower().Split(',');
            foreach (var iLanguage in _languageArray)
            {
                if (!allLanguageCodes.Contains(iLanguage))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
