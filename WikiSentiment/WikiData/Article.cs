using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WikiSentiment.DataObjects
{
    /// <summary>
    /// POJO class for a single wikipedia title
    /// </summary>
    public class Article
    {
        public string ttl { get; set; }

        public int vws { get; set; }

        public Dictionary<string, string> lngl { get; set; }    //linked articles in other languages

        /// <summary>
        /// Builds an article 
        /// </summary>
        /// <param name="_client">http client, wiki API headers required</param>
        /// <param name="_title">article title</param>
        /// <param name="_countryCode">two letter language code</param>
        /// <param name="_views">article views</param>
        /// <returns></returns>
        public static async Task<Article> Create(HttpClient _client, string _title, string _countryCode, int _views)
        {
            Article result = new Article()
            {
                ttl = _title,
                vws = _views,
                lngl = new Dictionary<string, string>()
            };

            //get english title of given article
            string langlinkTitle = await WikiAPIRequests.GetLangLink(_client, _title, _countryCode);

            //if failed to get english article, check if its a redirect and try again
            if (langlinkTitle != "")
            {
                result.lngl["en"] = langlinkTitle;
            }
            else
            {
                //if article is a redirect, fill in correct title and try to get a new langlink
                var redirectTitle = await WikiAPIRequests.GetRedirect(_client, _title, _countryCode);
                if (redirectTitle != "")
                {
                    result.ttl = redirectTitle;
                    var redirectLanglink = await WikiAPIRequests.GetLangLink(_client, redirectTitle, _countryCode);

                    if (redirectLanglink != "")
                        result.lngl["en"] = redirectLanglink;
                }
            }

            return result;
        }
    }
}
