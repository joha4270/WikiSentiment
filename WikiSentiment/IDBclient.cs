using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiSentiment
{
    public interface IDBClient
    {
        Task Upload(DateTime _date, string _content);
        Task<string> Load(DateTime _date);
    }
}
