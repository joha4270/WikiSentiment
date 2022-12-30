using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace WikiSentiment.WikiData
{
    /// <summary>
    /// POJO class for a collection of regional data for a single day
    /// </summary>
    public class DailyCollection
    {
        const int featuredArticles = 5;
        const int countryArticles = 5;
        public Dictionary<string, RegionalCollection> countrydailydict { get; set; }

        
        public List<string> featuredlist { get; set; }

        public async static Task<DailyCollection> Create(
           DateTime _date, string[] _countryCodes, Dictionary<string, string[]> _exceptions, 
           HttpClient _client, ILogger logger)
        {
            //start building daily data for each country asyncronously
            var regionalTasks = new Dictionary<string, Task<RegionalCollection>>();
            foreach (var iCountry in _countryCodes)
            {
                regionalTasks[iCountry] = RegionalCollection.BuildDailyData(
                        _client, _date, iCountry, _exceptions, countryArticles);
            }

            var regionalCollections = new Dictionary<string, RegionalCollection>();
            foreach (var iCountry in regionalTasks.Keys)
            {
                try { 
                    var result = await regionalTasks[iCountry]; 
                    regionalCollections[iCountry] = result; }
                catch (Exception e) {
                    logger.LogError($"Error building {iCountry}-collection for " +
                        $"{_date.Year}-{_date.Month:D2}-{_date.Day:D2}: {e.Message}"); }
            }

            var featuredCountries = getFeaturedCountries(regionalCollections, featuredArticles);

            return new DailyCollection()
            {
                countrydailydict = regionalCollections,
                featuredlist = featuredCountries
            }; ;
        }

        /// <summary>
        /// Updates old colection with new content, compiles new featured list
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="_newAdditions"></param>
        /// <returns></returns>
        public static DailyCollection CreateUpdated(DailyCollection _base, DailyCollection _newAdditions)
        {
            var newCollection = new Dictionary<string, RegionalCollection>(_base.countrydailydict);

            foreach(string iCountry in _newAdditions.countrydailydict.Keys)
            {
                newCollection[iCountry] = _newAdditions.countrydailydict[iCountry];
            }
            var featured = getFeaturedCountries(newCollection, featuredArticles);

            return new DailyCollection()
            {
                countrydailydict = newCollection,
                featuredlist = featured
            };
        }

        /// <summary>
        /// Get a list of top countries to with the most viewed articles
        /// </summary>
        /// <param name="_collection"></param>
        /// <param name="_featuredAmount"></param>
        /// <returns></returns>
        static List<string> getFeaturedCountries(
            Dictionary<string, RegionalCollection> _collection, int _featuredAmount)
        {
            //compile the list of country codes and viewership scores for top articles
            var countryTopArticlesData = new List<(string countrycode, float percentage)>();
            foreach (string iCountry in _collection.Keys)
            {
                countryTopArticlesData.Add((iCountry,
                    (100f * _collection[iCountry].articles[0].vws) / _collection[iCountry].totalviews));
            }

            //order data in the descending order
            countryTopArticlesData = (countryTopArticlesData.OrderBy(data => data.percentage)).Reverse().ToList();

            var amountToFeature = Math.Min(_featuredAmount, _collection.Count);
            List<string> result = new List<string>();

            //return countrycodes
            for (int i = 0; i < amountToFeature; i++)
                result.Add(countryTopArticlesData[i].countrycode);
            
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
            return countrydailydict.Count > 0;
        }
    }
}
