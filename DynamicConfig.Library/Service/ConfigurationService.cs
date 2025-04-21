using DynamicConfig.Library.Context;
using DynamicConfig.Library.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicConfig.Library.Service
{
    public class ConfigurationService
    {
        private readonly MongoDbContext _context;

        public ConfigurationService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<ConfigurationItem> GetConfigurationItemAsync(string name)
        {
            return await _context.ConfigurationItems
                .Find(x => x.Name == name && x.IsActive)
                .FirstOrDefaultAsync();
        }
    }

}
