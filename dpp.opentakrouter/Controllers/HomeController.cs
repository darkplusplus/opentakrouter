using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using dpp.opentakrouter.Models;
using Microsoft.Extensions.Configuration;

namespace dpp.opentakrouter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IClientRepository _clients;
        private readonly IMessageRepository _messages;
        private readonly IDataPackageRepository _datapackages;
        private readonly IConfiguration _configuration;
        
        public HomeController(ILogger<HomeController> logger, IDataPackageRepository datapackages, IClientRepository clients, IMessageRepository messages, IConfiguration configuration)
        {
            _logger = logger;
            _clients = clients;
            _messages = messages;
            _datapackages = datapackages;
            _configuration = configuration;
        }

        [Route("/")]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [Route("/map")]
        [HttpGet]
        public IActionResult Map()
        {
            ViewData.Add("ws-port", _configuration["server:websockets:port"] ?? "5000");
            return View();
        }

        [Route("/clients")]
        [HttpGet]
        public IActionResult Clients()
        {
            ViewData.Add("clients", _clients.Search().OrderBy(c => c.LastSeen));

            return View();
        }

        [Route("/datapackages")]
        [HttpGet]
        public IActionResult DataPackages()
        {
            ViewData.Add("datapackages", _datapackages.Search().OrderBy(dp => dp.SubmissionDateTime));

            return View();
        }


        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
