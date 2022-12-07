using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Net.Http;
using MyAnalyticsLibrary;

namespace ScraperFunction
{
    public class RedditJ
    {
        #region POCO
        class RootJ
        {
            public string kind { get; set; }
            public Data data { get; set; }
        }
        class Data
        {
            public string after { get; set; }
            public IEnumerable<Children> children { get; set; }
        }

        class Children
        {
            public ChildrenData data { get; set; }
        }

        class ChildrenData
        {
            public string title { get; set; }
        }
        #endregion

        string baseURI; //base reddit uri

        HttpClient reddit;zzzz

        public RedditJ(string _baseURI)
        {
            baseURI = _baseURI;

            //initialize http client with base URI
            reddit = new()
            {
                BaseAddress = new Uri(baseURI)
            };
        }

        public async Task<AnalyticsEntry> GetEntry(string _subreddit, string _sortingmethod = "hot")
        {
            if (_subreddit == null || _subreddit == "")
                throw new ArgumentException("Error: subreddit name is null or empty");

            if (_sortingmethod == null || _subreddit == "")
                throw new ArgumentException("Error: sorting method is null or empty");


            var rootJ = await reddit.GetFromJsonAsync<RootJ>(_subreddit + "/" + _sortingmethod + ".json");


            if (rootJ == null || rootJ.data == null || rootJ.data!.children == null)
                throw new Exception("Error parsing JSON file, incorrect schema applied");


            List<string> posts = new List<string>();

            for (int i = 0; i < rootJ.data.children.Count(); i++)
            {
                posts.Add(rootJ.data.children.ElementAt<Children>(i).data.title!);
            }


            return new AnalyticsEntry() { Subreddit = _subreddit, RawPosts = posts.ToArray() };
        }
        public async Task<Dictionary<string, List<string>>> GetWebData(string[] _subreddits, string _sortMethod = "hot")
        {

            if (_subreddits == null || _subreddits.Length == 0)
                throw new ArgumentException("Getting list of posts from subreddits, the given list is zero or empty");

            var result = new Dictionary<string, List<string>>();

            

            var taskDict = new Dictionary<string, Task<RootJ>>();

            for (int i = 0; i < _subreddits.Length; i++)
            {
                //send requests to map json files from the web using PACO
                var task = reddit.GetFromJsonAsync<RootJ>(_subreddits[i] + "/" + _sortMethod + ".json");

                //store tasks
                if (task != null) taskDict[_subreddits[i]] = task!;
            }

            foreach (string iSubreddit in taskDict.Keys)
            {
                try
                {
                    RootJ root = await taskDict[iSubreddit];
                    if (root == null || root.data == null || root.data!.children == null)
                        throw new Exception("Error parsing JSON file, incorrect schema applied");

                    result[iSubreddit] = new List<string>();

                    for (int i = 0; i < root.data.children.Count(); i++)
                    {
                        result[iSubreddit].Add(root.data.children.ElementAt<Children>(i).data.title!);
                    }
                }
                catch (Exception ex)
                {
                    result[iSubreddit] = new List<string>() { ex.Message };
                }
            }

            return result;

        }
    }
}
