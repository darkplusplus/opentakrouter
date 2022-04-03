using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace dpp.opentakrouter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly IRouter _router;

        public EventsController(ILogger<EventsController> logger, IRouter router)
        {
            _logger = logger;
            _router = router;
        }

        [Route("/api/events")]
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Xml)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult SubmitEvent()
        {
            try
            {
                using (var sr = new StreamReader(Request.BodyReader.AsStream()))
                {
                    var data = sr.ReadToEnd();
                    var evt = cot.Event.Parse(data);
                    _router.Send(evt, null);
                }
            }
            catch
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}