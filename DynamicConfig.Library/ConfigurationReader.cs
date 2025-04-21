using DynamicConfig.Library.Context;
using DynamicConfig.Library.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicConfig.Library
{
    public class ConfigurationReader
    {
        private readonly string _applicationName;
        private readonly int _refreshIntervalMs;
        private readonly IMongoCollection<ConfigurationItem> _configCollection;
        private Dictionary<string, ConfigurationItem> _configCache = new();
        private Timer _timer;

        public ConfigurationReader(string applicationName, string connectionString, int refreshIntervalMs)
        {
            _applicationName = applicationName;
            _refreshIntervalMs = refreshIntervalMs;

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("ConfigDb");
            _configCollection = database.GetCollection<ConfigurationItem>("ConfigurationItems");

            LoadConfigurationAsync().Wait(); 
            _timer = new Timer(_ => LoadConfigurationAsync().Wait(), null, _refreshIntervalMs, _refreshIntervalMs);
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                var filter = Builders<ConfigurationItem>.Filter.And(
                    Builders<ConfigurationItem>.Filter.Eq(c => c.ApplicationName, _applicationName),
                    Builders<ConfigurationItem>.Filter.Eq(c => c.IsActive, true)
                );

                var configs = await _configCollection.Find(filter).ToListAsync();

                var tempCache = configs.ToDictionary(c => c.Name, c => c);
                _configCache = tempCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Konfigürasyon yüklenemedi: " + ex.Message);
            }
        }
        private void StartRefreshing()
        {
            _timer = new Timer(async _ => await LoadConfigurationAsync(), null, _refreshIntervalMs, _refreshIntervalMs);
        }
        public T GetValue<T>(string key)
        {
            if (_configCache.TryGetValue(key, out var item))
            {
                try
                {
                    return (T)Convert.ChangeType(item.Value, typeof(T));
                }
                catch
                {
                    throw new InvalidCastException($"Key '{key}' için değer {item.Type} tipine dönüştürülemedi.");
                }
            }

            throw new KeyNotFoundException($"Key '{key}' bulunamadı.");
        }
    }
}
