using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace dpp.opentakrouter.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "internal")]
    [Route("/api/ui")]
    public class UiController : ControllerBase
    {
        private readonly IRouter _router;

        public UiController(IRouter router)
        {
            _router = router;
        }

        [HttpGet("events")]
        public IEnumerable<object> Events()
        {
            return _router
                .GetActiveEvents()
                .Select(evt => UiEventMessage.ToPayload(CotMessageEnvelope.FromEvent(evt)));
        }
    }
}
