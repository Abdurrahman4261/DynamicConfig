using DynamicConfig.Library.Context;
using DynamicConfig.Library.Entities;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

using DynamicConfig.Library.Context;
using DynamicConfig.Library.Entities;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicConfig.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAllOrigins")]
    public class ConfigController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public ConfigController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? name, [FromQuery] string? applicationName)
        {
            var filter = Builders<ConfigurationItem>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(name))
            {
                filter &= Builders<ConfigurationItem>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(name, "i"));
            }

            if (!string.IsNullOrWhiteSpace(applicationName))
            {
                filter &= Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName);
            }

            var result = await _context.ConfigurationItems.Find(filter).ToListAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ConfigurationItem item)
        {
            try
            {
                await _context.ConfigurationItems.InsertOneAsync(item);
                return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Konfigürasyon kaydederken hata oluştu.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] ConfigurationItem item)
        {
            var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.Id, id);
            var existing = await _context.ConfigurationItems.Find(filter).FirstOrDefaultAsync();

            if (existing is null) return NotFound();

            existing.Name = item.Name;
            existing.Type = item.Type;
            existing.Value = item.Value;
            existing.IsActive = item.IsActive;
            existing.ApplicationName = item.ApplicationName;

            var update = Builders<ConfigurationItem>.Update
                .Set(x => x.Name, existing.Name)
                .Set(x => x.Type, existing.Type)
                .Set(x => x.Value, existing.Value)
                .Set(x => x.IsActive, existing.IsActive)
                .Set(x => x.ApplicationName, existing.ApplicationName)
                .Set(x => x.Version, existing.Version + 1);

            await _context.ConfigurationItems.UpdateOneAsync(filter, update);
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.Id, id);
            var existing = await _context.ConfigurationItems.Find(filter).FirstOrDefaultAsync();

            if (existing is null) return NotFound();

            await _context.ConfigurationItems.DeleteOneAsync(filter);
            return NoContent();
        }
    }
}
