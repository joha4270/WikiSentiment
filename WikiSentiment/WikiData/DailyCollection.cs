using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace WikiSentiment.DataObjects
{
    /// <summary>
    /// POJO class for a collection of languages for a single day
    /// </summary>
    public class DailyCollection
    {
        const int featureArticles = 5; //amount of languages to put on featured list 

        const int keepArticles = 5;  //amount of articles to keep for each language

        public Dictionary<string, LanguageCollection> languagesDailyDict { get; set; }

        public List<string> featuredlist { get; set; }

        /// <summary>
        /// Build a collection for a day with given languages
        /// </summary>
        /// <param name="_date"></param>
        /// <param name="_languageCodes">Array of two letter language codes</param>
        /// <param name="_exceptions">Dictionary with exceptions in {"en": ["Main_Page", "Search"]} format</param>
        /// <param name="_client">Client for requests, wiki API headers required</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async static Task<DailyCollection> Create(
           DateTime _date, string[] _languageCodes, Dictionary<string, string[]> _exceptions, 
           HttpClient _client, ILogger logger)
        {
            //start daily data tasks for each country
            var regionalTasks = new Dictionary<string, Task<LanguageCollection>>();
            foreach (var iLanguage in _languageCodes)
            {
                regionalTasks[iLanguage] = LanguageCollection.Create(
                        _client, _date, iLanguage, _exceptions, keepArticles);
            }

            //try waiting for each task, log any errors
            var regionalCollections = new Dictionary<string, LanguageCollection>();
            foreach (var iCountry in regionalTasks.Keys)
            {
                try { 
                    var result = await regionalTasks[iCountry]; 
                    regionalCollections[iCountry] = result; }
                catch (Exception e) {
                    logger.LogError($"Error building {iCountry}-collection for " +
                        $"{_date.Year}-{_date.Month:D2}-{_date.Day:D2}: {e.Message}"); }
            }

            var featuredCountries = getFeaturedCountries(regionalCollections, featureArticles);

            return new DailyCollection()
            {
                languagesDailyDict = regionalCollections,
                featuredlist = featuredCountries
            }; ;
        }

        /// <summary>
        /// Updates old colection with new content, compiles new featured list
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="_newAdditions"></param>
        /// <returns></returns>
        public static DailyCollection UpdateGiven(DailyCollection _base, DailyCollection _newAdditions)
        {
            var newCollection = new Dictionary<string, LanguageCollection>(_base.languagesDailyDict);

            //override old data with new ones
            foreach(string iCountry in _newAdditions.languagesDailyDict.Keys)
            {
                newCollection[iCountry] = _newAdditions.languagesDailyDict[iCountry];
            }
            var featured = getFeaturedCountries(newCollection, featureArticles);

            return new DailyCollection()
            {
                languagesDailyDict = newCollection,
                featuredlist = featured
            };
        }

        /// <summary>
        /// Get ordered list of languages with the top viewed (proportionally) articles
        /// </summary>
        /// <param name="_collection"></param>
        /// <param name="_featuredAmount">amount of languages to keep</param>
        /// <returns>list of two letter language codes</returns>
        static List<string> getFeaturedCountries(
            Dictionary<string, LanguageCollection> _collection, int _featuredAmount)
        {
            //compile the list of country codes and viewership scores for top articles
            var languageTopArticlesData = new List<(string countrycode, float percentage)>();
            foreach (string iLanguage in _collection.Keys)
            {
                languageTopArticlesData.Add((iLanguage,
                    (100f * _collection[iLanguage].articles[0].vws) / _collection[iLanguage].totalviews));
            }

            //order data in the descending order
            languageTopArticlesData = (languageTopArticlesData.OrderBy(data => data.percentage)).Reverse().ToList();

            var amountToFeature = Math.Min(_featuredAmount, _collection.Count);
            List<string> result = new List<string>();

            //return countrycodes
            for (int i = 0; i < amountToFeature; i++)
                result.Add(languageTopArticlesData[i].countrycode);
            
            return result;
        }

        public string ToJSON()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            });
        }

        public bool IsValid()
        {
            return languagesDailyDict != null && languagesDailyDict.Count > 0;
        }
    }
}
