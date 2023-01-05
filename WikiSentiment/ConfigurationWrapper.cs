using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace WikiSentiment
{
    /// <summary>
    /// Wraps IConfiguration, allowing it to work with localsettings.json as well as Azure Configurations
    /// </summary>
    public class ConfigurationWrapper //TODO: implement as IConfiguration
    {
        private readonly IConfiguration config;

        public ConfigurationWrapper(IConfiguration _config)
        {
            config = _config;
        }

        /// <summary>
        /// Get value from configuration
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public T GetValue<T>(string key)
        {
            if (config.GetSection(key).Exists())
                return config.GetSection(key).Get<T>();

            else if (config.GetValue<string>(key) != null)
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(config.GetValue<string>(key));

            else
                throw new KeyNotFoundException($"No {key} key in appsettings");
        }
    }
}