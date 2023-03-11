using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WikiSentiment.DataObjects
{
    //POJO for collection of articles in single language
    public class LanguageCollection
    {
        public string countrycode { get; set; }

        public int totalviews { get; set; }

        public List<Article> articles { get; set; }

        /// <summary>
        /// Build daily collection of articles in single language
        /// </summary>
        /// <param name="_client">Http client for requests</param>
        /// <param name="_date"></param>
        /// <param name="_languageCode">Two letter code</param>
        /// <param name="_exceptions">Dictionary with exceptions in {"en": ["Main_Page", "Search"]} format</param>
        /// <param name="_keepMax">keep X amount of articles</param>
        /// <returns></returns>
        public static async Task<LanguageCollection> Create(
            HttpClient _client, DateTime _date,
            string _languageCode, Dictionary<string, string[]> _exceptions,
            int _keepMax)
        {
            //get total views for a regional wikipedia
            var totalviews = await WikiAPIRequests.GetTotalViews(_client, _languageCode, _date);

            //get raw (title, views) data
            var articleEntries = await WikiAPIRequests.GetArticleList(_client, _languageCode, _date);

            var createdArticles = new Dictionary<string, Article>();

            foreach (var iEntry in articleEntries)
            {
                //stop when created enough articles
                if (createdArticles.Count >= _keepMax)
                    break;

                //if the article name is not on the exceptions list
                if (!isException(iEntry.title, _languageCode, _exceptions))
                {
                    //if already createed an article with this title,
                    //add its views to the record. otherwise make a new article
                    bool hasThisTitle = createdArticles.ContainsKey(iEntry.title);

                    if (hasThisTitle)
                        createdArticles[iEntry.title].vws += iEntry.views;

                    else
                    {
                        var article = await Article.Create(_client, iEntry.title, _languageCode, iEntry.views);
                        createdArticles[article.ttl] = article;
                    }
                }
            }

            return new LanguageCollection()
            {
                countrycode = _languageCode,
                totalviews = totalviews,
                articles = createdArticles.Values.ToList()
            };
        }

        /// <summary>
        /// Returns bool if given title is among exceptions
        /// </summary>
        /// <param name="_title">String of a title</param>
        /// <param name="_language">Two letter language code</param>
        /// <param name="exceptions">Dictionary with exceptions in {"en": ["Main_Page", "Search"]} format</param>
        /// <returns></returns>
        static bool isException(string _title, string _language, Dictionary<string, string[]> exceptions)
        {
            var title = _title.ToLower();
            //check regional exception array
            if (exceptions.ContainsKey(_language))
            {
                foreach (string iException in exceptions[_language])
                    if (title.Contains(iException))
                        return true;
            }

            //check common exceptions array (all contains . or :)
            if (title.Contains('.') | title.Contains(':'))
            {
                foreach (string iPartException in exceptions["all"])
                    if (title.Contains(iPartException))
                        return true;
            }

            return false;
        }
    }
}
