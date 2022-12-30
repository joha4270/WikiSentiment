using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace WikiSentiment.WikiData
{
    /// <summary>
    /// POJO class representing multiple days of data
    /// </summary>
    public class MonthlyCollection
    {
        public Dictionary<string, DailyCollection> datadict { get; set; }
        public string date { get; set; }

        public string ToJSON()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            });
        }

        public static MonthlyCollection Create(string _date)
        {
            return new MonthlyCollection() { date = _date, datadict = new Dictionary<string, DailyCollection>() };
        }
    }
}
