using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Text;
using WikiSentiment.DataObjects;

namespace WikiSentiment
{
    public static class DBUpdates
    {
        /// <summary>
        /// Creates article collections for given languages. Goes backward from given date
        /// </summary>
        /// <param name="_date">Starting date</param>
        /// <param name="_daysToGo">Collections to create</param>
        /// <param name="_discardOldEntries">If true will not discard all existing daily data in the DB</param>
        /// <param name="_languageCodes">list of two letter language codes</param>
        /// <param name="_exceptions"></param>
        /// <param name="_httpClient">http client with API headers</param>
        /// <param name="_dbClient"></param>
        /// <param name="_logger"></param>
        /// <returns></returns>
        public static async Task updateDatabase(DateTime _date, int _daysToGo, bool _discardOldEntries,
            string[] _languageCodes, Dictionary<string, string[]> _exceptions, 
            HttpClient _httpClient, IDBClient _dbClient, ILogger _logger)
        {
            for (int i = _daysToGo; i > 0; i--)
            {
                var newCollection = await DailyCollection.Create(_date, _languageCodes, 
                    _exceptions, _httpClient, _logger);

                //if not discarding old data, use it as a base for new collection
                if (!_discardOldEntries)
                {
                    var dbRequest = await _dbClient.Load(_date);
                    DailyCollection? oldDaily = null;
                    try
                    {
                        oldDaily = JsonSerializer.Deserialize<DailyCollection>(dbRequest);
                    }
                    catch (Exception _e)
                    {
                        _logger.LogError($"Error reading previous data, overwriting it " +
                            $"{_date.Year}-{_date.Month}-{_date.Day:D2}: "+ $"{_e.ToString()}");
                    }

                    if (oldDaily != null && oldDaily.IsValid())
                        newCollection = DailyCollection.UpdateGiven(oldDaily, newCollection);
                }


                if (newCollection.IsValid())
                    await _dbClient.Upload(_date, newCollection.ToJSON());
                else
                    _logger.LogError($"Skipped uploading collection " +
                        $"{_date.Year}-{_date.Month}-{_date.Day:D2}");

                _date = _date.AddDays(-1);
            }
        }
    }
}
