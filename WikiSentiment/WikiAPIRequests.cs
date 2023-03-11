using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace WikiSentiment
{
    /// <summary>
    /// Collection of API requests that parse json data
    /// </summary>
    public static class WikiAPIRequests
    {
        /// <summary>
        /// Gets english title of given article. Returns empty string if none exist
        /// </summary>
        /// <param name="_client">http client, api headers required</param>
        /// <param name="_title"></param>
        /// <param name="_countryCode">two letter language code</param>
        /// <returns>English title, returns empty if none</returns>
        public static async Task<string> GetLangLink(
                HttpClient _client, string _title, string _countryCode)
        {
            string languageTarget = "en";
            _title = stripSubmenuLink(_title);
            JsonNode langlinkNode;

            string url = $"https://{_countryCode}.wikipedia.org/w/api.php?action=query&titles=" +
                $"{_title}&prop=langlinks&format=json&lllang={languageTarget}";

            var langlinkResponse = await _client.GetStringAsync(url);
            var jsonObject = JsonNode.Parse(langlinkResponse);

            //validate json response, return empty string if failed
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

        /// <summary>
        /// Returns title, if given article is a redirect page. Returns empty if not
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="_title"></param>
        /// <param name="_country">two letter language code</param>
        /// <returns>Title of a redirect page, returns empty if not</returns>
        public static async Task<string> GetRedirect(
            HttpClient _client, string _title, string _country)
        {
            _title = stripSubmenuLink(_title);
            var request = await _client.GetStringAsync(
                $"https://{_country}.wikipedia.org/w/api.php?action=parse&" +
                $"formatversion=2&page={_title}&prop=wikitext&format=json");

            JsonObject jObject = JsonNode.Parse(request).AsObject();

            //validate return json
            if (!jObject.ContainsKey("parse") ||
                !jObject["parse"].AsObject().ContainsKey("wikitext"))
            {
                return "";
            }

            //extract title, redirect article text format is "#REDIRECT [[original_title]]"
            string articleText = ((string)jObject["parse"].AsObject()["wikitext"]).Trim();
            if (articleText.StartsWith("#") &&
                articleText.Contains("[[") &&
                articleText.Contains("]]"))
            {
                return normalizeTitle(articleText.Split("[[", 2)[1].Split("]]")[0]);
            }

            return "";
        }

        /// <summary>
        /// Get top pairs of (title, views) for given language
        /// </summary>
        /// <param name="_client">Http client with API headers</param>
        /// <param name="_languageCode">Two letter country code</param>
        /// <param name="_date">Date for lookup</param>
        /// <returns>Tuple with articleArray(name, views) of articles</returns>
        public static async Task<(string title, int views)[]>
            GetArticleList(HttpClient _client, string _languageCode, DateTime _date)
        {
            var articlesString = await _client.GetStringAsync(
                    "https://wikimedia.org/api/rest_v1/metrics/pageviews/" +
                    $"top/{_languageCode}.wikipedia.org/all-access/{_date.Year}/{_date.Month:D2}/{_date.Day:D2}");

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

        /// <summary>
        /// Gets total daily views for given language
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="_languageCode"></param>
        /// <param name="_date"></param>
        /// <returns></returns>
        public static async Task<int> GetTotalViews(
            HttpClient _client, string _languageCode, DateTime _date)
        {
            var viewsString = await _client.GetStringAsync(
                    "https://wikimedia.org/api/rest_v1/metrics/pageviews/" +
                    $"aggregate/{_languageCode}.wikipedia.org/all-access/user/daily/" +
                    $"{_date.Year}{_date.Month:D2}{_date.Day:D2}/{_date.Year}{_date.Month:D2}{_date.Day:D2}");

            JsonObject jObject = JsonNode.Parse(viewsString).AsObject();

            //validate object and return total views
            if (jObject.ContainsKey("items") &&
                jObject["items"].AsArray().Count > 0 &&
                jObject["items"][0].AsObject().ContainsKey("views") )
            {
                return (int)JsonNode.Parse(viewsString).AsObject()["items"][0]["views"];
            }

            throw new HttpRequestException($"Http response has bad format. Date: {_date}; language:{_languageCode}");
        }

        
        static string normalizeTitle(string _title)
        {

            return _title.Trim().Replace(' ', '_');
        }

        /// <summary>
        /// Strip subitems from a title (in "/wiki/Albert_Einstein#Childhood", #Childhood is a subitem)
        /// </summary>
        /// <param name="_title"></param>
        /// <returns></returns>
        static string stripSubmenuLink(string _title)
        {
            if (_title.Contains('#'))
                return _title.Split('#', 2)[0];
            else
                return _title;
        }
    }
}
