using Azure;
using Azure.Data.Tables;

namespace WikiSentiment
{
    public interface IDBClient
    {
        Task Upload(string _YYYYMM, string _DD, string _content);
        Task<string> Load(string _YYYYMM, string _DD);
    }

    public class StorageAPI : IDBClient
    {
        TableClient client;
        const string pKey = "wikidata";
        public StorageAPI(TableClient _client)
        {
            client = _client;
        }

        public async Task<string> Load(string _YYYYMM, string _DD)
        {
            AsyncPageable<DayEntity> result = client.QueryAsync<DayEntity>(
                e => e.PartitionKey == _YYYYMM && e.RowKey == _DD);
            await foreach (var res in result)
            {
                return res.DailyData;
            }

            return "";
        }

        public async Task Upload(string _YYYYMM, string _DD, string _dailyData)
        {
            var dayEntity = new DayEntity() { PartitionKey = _YYYYMM, RowKey = _DD, DailyData = _dailyData };

            await client.UpsertEntityAsync(dayEntity);
        }
    }

    class DayEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public string DailyData { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public DayEntity()
        {

        }
    }
}
