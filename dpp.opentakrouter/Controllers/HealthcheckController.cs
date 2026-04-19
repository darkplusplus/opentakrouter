using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace dpp.opentakrouter.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "internal")]
    [Route("[controller]")]
    public class HealthcheckController : ControllerBase
    {
        private readonly ILogger<HealthcheckController> _logger;

        public HealthcheckController(ILogger<HealthcheckController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public object Get()
        {
            return new Dictionary<string, string>()
            {
                { "status", "ok" },
            };
        }
    }
}
