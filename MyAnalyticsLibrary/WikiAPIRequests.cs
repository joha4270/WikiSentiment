using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace WikiSentiment
{
    public static class WikiAPIRequests
    {
        public static async Task<string> GetLangLink(
                HttpClient _client, string _title, string _countryCode)
        {
            _title = stripSubcategories(_title);
            JsonNode langlinkNode;
            string url = $"https://{_countryCode}.wikipedia.org/w/api.php?action=query&titles=" +
                $"{_title}&prop=langlinks&format=json&lllang=en";

            var langlinkResponse = await _client.GetStringAsync(url);
            var jsonObject = JsonNode.Parse(langlinkResponse);
            if (!jsonObject.AsObject().ContainsKey("query") ||
                !JsonNode.Parse(langlinkResponse).AsObject()["query"].AsObject().ContainsKey("pages"))
                return "";

            langlinkNode = JsonNode.Parse(langlinkResponse).AsObject()["query"]["pages"];

            var articleID = JsonSerializer.Deserialize<Dictionary<string, JsonNode>>(langlinkNode).Keys.First();

            //if article has an english counterpart, it'll have a langlink
            if (langlinkNode[articleID].AsObject().ContainsKey("langlinks"))
            {
                //TODO add conditions
                return normalizeTitle((string)langlinkNode[articleID]["langlinks"][0]["*"]);
            }
            else
                return "";
        }

        public static async Task<string> GetRedirect(
            HttpClient _client, string _title, string _country)
        {
            _title = stripSubcategories(_title);
            var request = await _client.GetStringAsync(
                $"https://{_country}.wikipedia.org/w/api.php?action=parse&" +
                $"formatversion=2&page={_title}&prop=wikitext&format=json");
            JsonObject jObject = JsonNode.Parse(request).AsObject();

            if (jObject.ContainsKey("parse") &&
                jObject["parse"].AsObject().ContainsKey("wikitext"))
            {
                string articleText = ((string)jObject["parse"].AsObject()["wikitext"]).Trim();
                if (articleText.StartsWith("#") &&
                    articleText.Contains("[[") &&
                    articleText.Contains("]]"))
                {
                    return normalizeTitle(articleText.Split("[[", 2)[1].Split("]]")[0]);
                }
            }

            return "";
        }

        /// <summary>
        /// Send a GET request for a list of popular articles in a regional wikipedia
        /// </summary>
        /// <param name="_client">Http client with API headers</param>
        /// <param name="_countryCode">Two letter country code</param>
        /// <param name="_date">Date for lookup</param>
        /// <returns>Tuple with articleArray(name, views) of articles</returns>
        public static async Task<(string title, int views)[]>
            GetArticleList(HttpClient _client, string _countryCode, DateTime _date)
        {

            var articlesString = await _client.GetStringAsync(
                    "https://wikimedia.org/api/rest_v1/metrics/pageviews/" +
                    $"top/{_countryCode}.wikipedia.org/all-access/{_date.Year}/{_date.Month:D2}/{_date.Day:D2}");
            var jArticleArray = JsonNode.Parse(articlesString).AsObject()["items"][0]["articles"].AsArray();

            var articleArray = new (string title, int views)[jArticleArray.Count];

            for (int i = 0; i < jArticleArray.Count; i++)
            {
                articleArray[i] = (
                    normalizeTitle((string)jArticleArray[i]["article"].AsValue()), 
                    (int)jArticleArray[i]["views"].AsValue());
            }

            return articleArray;
        }

        public static async Task<int> GetTotalViews(
            HttpClient _client, string _countryCode, DateTime _date)
        {
            var viewsString = await _client.GetStringAsync(
                    "https://wikimedia.org/api/rest_v1/metrics/pageviews/" +
                    $"aggregate/{_countryCode}.wikipedia.org/all-access/user/daily/" +
                    $"{_date.Year}{_date.Month:D2}{_date.Day:D2}/{_date.Year}{_date.Month:D2}{_date.Day:D2}");

            return (int)JsonNode.Parse(viewsString).AsObject()["items"][0]["views"];
        }

        static string normalizeTitle(string _title)
        {

            return _title.Trim().Replace(' ', '_');
        }

        static string stripSubcategories(string _title)
        {
            if (_title.Contains('#'))
                return _title.Split('#', 2)[0];
            else
                return _title;
        }
    }
}
