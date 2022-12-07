using System.Text.Json.Serialization;

namespace MyAnalyticsLibrary
{
    public class AnalyticsEntry
    {
        public string Subreddit { get; set; }
        public string[] RawPosts { get; set; }
    }

    public class AnalyticsEntryProcessed
    {
        public string Subreddit { get; set; }
        public string[] RawPosts { get; set; }

        public string[] ProcessedPosts { get; set; }

    }
}