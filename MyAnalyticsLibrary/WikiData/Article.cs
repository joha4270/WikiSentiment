using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WikiSentiment.WikiData
{
    /// <summary>
    /// POJO class for a single wikipedia title
    /// </summary>
    public class Article
    {
        //public string cde { get; set; }

        public string ttl { get; set; }

        public int vws { get; set; }

        public Dictionary<string, string> lngl { get; set; }

        public static async Task<Article> Create(HttpClient _client, string _title, string _countryCode, int _views)
        {
            Article result = new Article()
            {
                ttl = _title,
                vws = _views,
                lngl = new Dictionary<string, string>()
            };

            //article.link = $"https://{_countryCode}.wikipedia.org/wiki/{article.ttl}";
            string langlinkTitle = await WikiAPIRequests.GetLangLink(_client, _title, _countryCode);


            if (langlinkTitle != "")
            {
                result.lngl["en"] = langlinkTitle;
            }
            else
            {
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
