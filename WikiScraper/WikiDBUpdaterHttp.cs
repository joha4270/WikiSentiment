using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using Azure.Data.Tables;
using WikiSentiment.WikiData;


namespace WikiSentiment
{
    public class WikiDBUpdaterHttp
    {
        readonly IConfiguration config;

        IDBClient dbClient;
        HttpClient httpClient;

        string[] allLanguageCodes;
        Dictionary<string, string[]> articleExceptions;

        public WikiDBUpdaterHttp(IConfiguration iConfig)
        {
            config = iConfig;
            articleExceptions = config.GetSection("FunctionValues:CountryExceptions").
                Get<Dictionary<string, string[]>>(); 

            allLanguageCodes = config.GetSection("FunctionValues:CountryCodes").Get<List<string>>().ToArray();

            httpClient = getHttpClient(config);
        }


        [FunctionName("WikiDBUpdaterHttp")]

        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("datatables"), StorageAccount("AzureWebJobsStorage")] TableClient client, 
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


            string YYYYMMDD = req.Query["date"];
            DateTime startDate;
            if (YYYYMMDD == null)
                return new OkObjectResult("Missing parameter: date");
            if (!validateDate(YYYYMMDD, out startDate))
                return new OkObjectResult("Bad starting date: " + YYYYMMDD);


            int daysToGo = 1;
            string daysToGoString = req.Query["days"];
            if (daysToGoString != null)
                if (!int.TryParse(daysToGoString, out daysToGo))
                    return new OkObjectResult("Bad days parameter: " + daysToGoString);


            string[] languages = allLanguageCodes;
            string languageStrings = req.Query["languages"];
            if (languageStrings != null)
                if (!validateLanguages(languageStrings, out languages))
                    return new OkObjectResult("Bad languages parameter: " + languageStrings);

            bool discardOldData = false;
            string discardString = req.Query["discard"];
            if (discardString != null)
                discardOldData = true;


            dbClient = new StorageAPI(client);

            await updateDatabase(startDate, daysToGo, discardOldData, languages, articleExceptions, log);
            
            return new OkObjectResult("all done ^_^");
        }

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

        async Task updateDatabase(DateTime _date, int _daysToGo, bool _discardOldEntries,
            string[] _languageCodes, Dictionary<string, string[]> _exceptions, ILogger _logger)
        {
            for (int i = _daysToGo; i > 0; i--)
            {
                string YYYYMM = $"{_date.Year}-{_date.Month:D2}";

                var newDaily = await DailyCollection.Create(_date, _languageCodes, _exceptions, httpClient, _logger);

                if (!_discardOldEntries)
                {
                    var dbRequest = await dbClient.Load(YYYYMM, $"{_date.Day:D2}");
                    var oldDaily = JsonSerializer.Deserialize<DailyCollection>(dbRequest);
                    if (oldDaily.IsValid())
                        newDaily = DailyCollection.CreateUpdated(oldDaily, newDaily);
                }
                

                if (newDaily.IsValid())
                {
                    await dbClient.Upload(YYYYMM, $"{_date.Day:D2}", newDaily.ToJSON());
                }
                else
                    _logger.LogError($"Skipped uploading collection " +
                        $"{_date.Year}-{_date.Month}-{_date.Day:D2}");

                _date = _date.AddDays(-1);
            }
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
    }
}
