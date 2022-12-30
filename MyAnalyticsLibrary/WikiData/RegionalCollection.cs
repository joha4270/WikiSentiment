using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WikiSentiment.WikiData
{
    //a collection of processed data about one wiki for a single day
    public class RegionalCollection
    {
        public string countrycode { get; set; }

        public int totalviews { get; set; }

        public List<Article> articles { get; set; }

        /// <summary>
        /// build data collection about a single language wiki project
        /// </summary>
        /// <param name="_countryCode"></param>
        /// <param name="_exceptions"></param>
        /// <param name="_totalviews"></param>
        /// <param name="_articles"></param>
        /// <param name="_keepMax"></param>
        /// <returns></returns>
        public static async Task<RegionalCollection> BuildDailyData(
            HttpClient _client, DateTime _date,
            string _countryCode, Dictionary<string, string[]> _exceptions,
            int _keepMax)
        {

            var totalviews = await WikiAPIRequests.GetTotalViews(_client, _countryCode, _date);

            var articleEntries = await WikiAPIRequests.GetArticleList(_client, _countryCode, _date);

            var createdArticles = new Dictionary<string, Article>();

            foreach (var iEntry in articleEntries)
            {
                if (createdArticles.Count >= _keepMax)
                    break;

                //TODO: add more comprehensive check
                //if the article name is not on the exceptions list
                if (!isException(iEntry.title, _countryCode, _exceptions))
                {
                    //if article has this title, add its views to the record. otherwise make a new article
                    bool hasThisTitle = createdArticles.ContainsKey(iEntry.title);

                    if (hasThisTitle)
                        createdArticles[iEntry.title].vws += iEntry.views;
                    else
                    {
                        var article = await Article.Create(_client, iEntry.title, _countryCode, iEntry.views);
                        createdArticles[article.ttl] = article;
                    }
                }
            }

            return new RegionalCollection()
            {
                countrycode = _countryCode,
                totalviews = totalviews,
                articles = createdArticles.Values.ToList()
            };
        }

        static bool isException(string _title, string _country, Dictionary<string, string[]> exceptions)
        {
            var title = _title.ToLower();
            if (exceptions.ContainsKey(_country))
            {
                foreach (string iException in exceptions[_country])
                    if (title.Contains(iException))
                        return true;
            }
            if (title.Contains('.')| title.Contains(':'))
                foreach (string iPartException in exceptions["parts"])
                    if (title.Contains(iPartException))
                        return true;
            return false;
        }
    }
}
