using Microsoft.AspNetCore.Mvc;

namespace DynamicConfig.API.Controllers
{
    public class DebugController : Controller
    {
        private readonly IConfiguration _config;
        public DebugController(IConfiguration config) => _config = config;

        [HttpGet("conn")]
        public string GetConnString() => _config.GetConnectionString("DefaultConnection");
    }
}
