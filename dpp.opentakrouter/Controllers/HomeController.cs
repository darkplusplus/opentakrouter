﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using dpp.opentakrouter.Models;

namespace dpp.opentakrouter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        IClientRepository _clients;
        IMessageRepository _messages;
        IDataPackageRepository _datapackages;
        
        public HomeController(ILogger<HomeController> logger, IDataPackageRepository datapackages, IClientRepository clients, IMessageRepository messages)
        {
            _logger = logger;
            _clients = clients;
            _messages = messages;
            _datapackages = datapackages;
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