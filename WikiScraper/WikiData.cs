using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using static WikiScraper.WikiData;
using System.Collections;

namespace WikiScraper
{
    //entry for daily data about wikipedia project with an article split
    public static class WikiData
    {
        //collection of data for an entire day
        public class DailyCollection
        { 
            public List<DailyData> wikiDataList { get; set; }

            public List<FeaturedArticle> featuredList { get; set; }

            public string date { get; set; }

            public async static Task<DailyCollection> BuildCollection(
               DateTime _date, string[] _countryCodes, Dictionary<string, List<string>> _exceptions, HttpClient _client)
            {
                var resultCollection = new DailyCollection()
                {
                    wikiDataList = new List<DailyData>(),
                    featuredList = new List<FeaturedArticle>(),
                    date = $"{ _date.Year}{_date.Month:D2}{_date.Day:D2}"
                };

                foreach (string iCountry in _countryCodes)
                {
                    
                    var viewsString = await _client.GetStringAsync(
                        "https://wikimedia.org/api/rest_v1/metrics/pageviews/" +
                        $"aggregate/{iCountry}.wikipedia.org/all-access/user/daily/" +
                        $"{_date.Year}{_date.Month:D2}{_date.Day:D2}/{_date.Year}{_date.Month:D2}{_date.Day:D2}");
                    var viewsObj = JsonNode.Parse(viewsString).AsObject();
                    var totalviews = viewsObj["items"][0]["views"];

                    //REST request:
                    //wikimedia.org/api/rest_v1/metrics/pageviews/aggregate/ru.wikipedia.org/all-access/user/daily/20221120/20221120
                    var articlesString = await _client.GetStringAsync(
                        "https://wikimedia.org/api/rest_v1/metrics/pageviews/" +
                        $"top/{iCountry}.wikipedia.org/all-access/{_date.Year}/{_date.Month:D2}/{_date.Day:D2}");

                    var articlesObj = JsonNode.Parse(articlesString).AsObject();
                    var articles = articlesObj["items"][0]["articles"];

                    resultCollection.wikiDataList.Add(
                        DailyData.BuildDailyData(iCountry, _exceptions[iCountry].ToArray(), (int)totalviews.AsValue(),
                        articles, 10));
                }


                List<FeaturedArticle> popularArticles = new List<FeaturedArticle>();
                int topAmount = 3;

                foreach (DailyData iDaily in resultCollection.wikiDataList)
                {
                    if (iDaily.articles.Count > 0)
                        popularArticles.Add(new FeaturedArticle()
                        {
                            name = iDaily.articles[0].name,
                            views = iDaily.articles[0].views,
                            popularityIndex = iDaily.articles[0].popularityIndex,
                            countryCode = iDaily.countrycode
                        });
                }

                //order articles by popularity and flip em
                popularArticles = popularArticles.OrderBy(article => article.popularityIndex).Reverse().ToList().
                    GetRange(0, popularArticles.Count > topAmount ? topAmount : popularArticles.Count);

                //get langlinks to english article
                foreach(FeaturedArticle article in popularArticles)
                {
                    article.oglink = "https://" + article.countryCode + ".wikipedia.org/wiki/" + article.name;
                    if (article.countryCode == "en")
                        article.enlink = article.oglink;
                    else
                    {
                        var langlinks = await _client.GetStringAsync(
                            $"https://{article.countryCode}.wikipedia.org/w/api.php?action=query&titles={article.name}&prop=langlinks&format=json&lllang=en");
                        var langlinkObj = JsonNode.Parse(langlinks).AsObject();

                        //couldnt get the key other than through enumerator
                        IEnumerator enumerator = langlinkObj["query"]["pages"].AsObject().GetEnumerator();
                        enumerator.MoveNext();
                        var test = ((KeyValuePair<string, JsonNode>)enumerator.Current);
                        var test2 = test.Value["langlinks"];
                        var test3 = test2[0];
                        var test4 = test3["*"];
                        article.enlink = "https://en.wikipedia.org/wiki/" + test4;
                    }
                    
                    
                }
                resultCollection.featuredList = popularArticles;

                return resultCollection;
            }

            public override string ToString()
            {
                string result = "Daily Data for " + date;
                result += '\n' + "Featured articles: " + '\n';
                foreach (FeaturedArticle iFeatured in featuredList)
                {
                    result += iFeatured.ToString() + '\n';
                }
                return result;
            }
        }

        //a collection of processed data about one wiki for a single day
        public class DailyData
        {
            public string countrycode { get; set; }

            public int totalviews { get; set; }

            public List<Article> articles { get; set; }

            public override string ToString()
            {
                string result = $"{countrycode}.wikipedia.org was visisted {totalviews} times that day" + 
                    + '\n' + "top 10 articles:";
                int limit = articles.Count < 10 ? articles.Count : 10;
                for (int i = 0; i < limit; i++)
                {
                    result += '\n';
                    result += articles[i].name + '(' + articles[i].views + ')' + $"{articles[i].popularityIndex:F3}";
                }
                return result;
            }

            /// <summary>
            /// build data collection about a single language wiki project
            /// </summary>
            /// <param name="_countryCode"></param>
            /// <param name="_exceptions"></param>
            /// <param name="_totalviews"></param>
            /// <param name="_articles"></param>
            /// <param name="_keepMax"></param>
            /// <returns></returns>
            public static DailyData BuildDailyData(
                string _countryCode, string[] _exceptions,
                int _totalviews, JsonNode _articles,
                int _keepMax)
            {

                var result = new DailyData()
                {
                    countrycode = _countryCode,
                    totalviews = _totalviews,
                    articles = new List<Article>()
                };

                var articleArray = _articles.AsArray();

                for (int i = 0; i < articleArray.Count; i++)
                {
                    var node = articleArray[i];

                    var article = new Article()
                    {
                        name = (string)node["article"].AsValue(),
                        views = (int)node["views"].AsValue()
                    };

                    if (!_exceptions.Contains(article.name))
                    {
                        article.popularityIndex = calculatePopIndex(result.totalviews, article.views);
                        result.articles.Add(article);
                    }

                    if (result.articles.Count >= _keepMax)
                        break;
                }

                return result;
            }

            //what percentage out of total views this article takes
            static float calculatePopIndex(int _totalViews, int _articleViews)
            {
                return 100f * _articleViews / (_totalViews);
            }
        }
        public class Article
        {
            public string name { get; set; }

            public int views { get; set; }

            public float popularityIndex { get; set; }
        }

        public class FeaturedArticle : Article
        {
            public string countryCode { get; set; }

            public string enlink { get; set; }

            public string oglink { get; set; }

            public override string ToString()
            {
                string result = $"{name} ({views}, {popularityIndex})" +'\n' +
                    $"{oglink}" + '\n' +
                    $"{enlink}";
                return result;
            }
        }
    }
}
