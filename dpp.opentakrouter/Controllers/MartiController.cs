using dpp.opentakrouter.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dpp.opentakrouter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MartiController : ControllerBase
    {
        private readonly ILogger<MartiController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDataPackageRepository _datapackages;

        private string _endpoint = "localhost";
        private int _port = 8080;

        public MartiController(ILogger<MartiController> logger, IConfiguration configuration, IDataPackageRepository datapackages)
        {
            _logger = logger;
            _configuration = configuration;
            _datapackages = datapackages;

            _endpoint = _configuration.GetValue("server:web:endpoint", Dns.GetHostName());
            _port = _configuration.GetValue("server:web:port", 8080);
        }

        [Route("/Marti/api/clientEndPoints")]
        [HttpGet]
        public object ClientEndpoints()
        {
            // TODO: Figure out mgmt context to keep track of client endpoints
            /*
            var endpoint = new Dictionary<string, string>()
            {
                { "lastEventTime", "2020-01-31T15:30:00.000Z" },
                { "lastStatus", "Connected" },
                { "uid", "asdf" },
                { "callsign": "GOOSE" },
            }
            */

            return new Dictionary<string, object>()
            {
                { "Matcher", "com.bbn.marti.remote.ClientEndpoint" },
                { "BaseUrl", "" },
                { "ServerConnectionString", _configuration.GetValue("server:public_endpoint", "") },
                { "NotificationId", "" },
                { "type", "com.bbn.marti.remote.ClientEndpoint" },
                { "data", new List<object>() },
            };
        }

        [Route("/Marti/api/version")]
        [HttpGet]
        public string Version()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return $"opentakrouter-{version}";
        }

        [Route("/Marti/api/version/config")]
        [HttpGet]
        public object VersionConfig()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var hostname = _configuration.GetValue("server:endpoint", Dns.GetHostName());
            var node_id = _configuration.GetValue("server:id", Dns.GetHostName());

            return new Dictionary<string, object>()
            {
                { "version", "2" },
                { "type", "ServerConfig" },
                { "data", new Dictionary<string, object>(){
                    { "version", $"opentakrouter-{version}" },
                    { "api", "2" },
                    { "hostname", hostname },
                }},
                { "nodeId", node_id },
            };
        }

        [Route("/Marti/sync/search")]
        [HttpGet]
        public object SearchDatapackages(string? keywords="", string? tool="")
        {
            // TODO: add logic for `tool` param to control package privacy

            List<Dictionary<string, object>> packages = new List<Dictionary<string, object>>();
            foreach (var dp in _datapackages.Search(keywords))
            {
                packages.Add(new Dictionary<string, object>()
                {
                    { "UID", dp.UID },
                    { "Name", dp.Name },
                    { "Hash", dp.Hash },
                    { "PrimaryKey", dp.PrimaryKey },
                    { "SubmissionDateTime", $"{dp.SubmissionDateTime.ToUniversalTime():u}" },
                    { "SubmissionUser", dp.SubmissionUser },
                    { "CreatorUid", dp.CreatorUid },
                    { "Keywords", dp.Keywords },
                    { "MIMEType", dp.MIMEType },
                    { "Size", dp.Size },
                    { "Visibility", dp.IsPrivate ? "private" : "public" }
                });
            }
            
            return new Dictionary<string, object>()
            {
                { "resultCount", packages.Count },
                { "results", packages }
            };
        }

        [Route("/Marti/sync/content")]
        [HttpGet]
        public IActionResult GetDatapackage(string hash)
        {
            try
            {
                var dp = _datapackages.Get(hash);

                return new FileContentResult(dp.Content, dp.MIMEType)
                {
                    FileDownloadName = dp.UID
                };
            }
            catch(Exception e)
            {
                return NotFound();
            }
        }

        [Route("/Marti/sync/missionupload")]
        [HttpPost, DisableRequestSizeLimit]
        public IActionResult UploadDatapackage(string hash, string filename, string creatorUid, string? keywords="missionpackage", string? visibility="private")
        {
            try
            {
                var user = Request.Headers.ContainsKey("X-USER") 
                    ? Request.Headers["X-USER"].ToString() 
                    : "Anonymous";

                var file = Request.Form.Files[0];

                _datapackages.Add(file, hash, filename, user, creatorUid, keywords, visibility);
            }
            catch(Exception e)
            {
                return StatusCode(500, $"{e.Message}");
            }

            return Ok($"https://{_endpoint}:{_port}/Marti/sync/content?hash={hash}");
        }

        [Route("/Marti/api/sync/metadata/{hash}/tool")]
        [HttpPut]
        public IActionResult UpdateDatapackageMetadata(string hash)
        {
            try
            {
                var dp = _datapackages.Get(hash);
                dp.IsPrivate = !Request.Body.ToString().Contains("public");
                _datapackages.Update(dp);

                return Ok($"https://{_endpoint}:{_port}/Marti/sync/content?hash={hash}");
            }
            catch
            {
                return NotFound();
            }
        }

        [Route("/Marti/sync/missionquery")]
        [HttpGet]
        public IActionResult DatapackageExists(string hash)
        {
            try
            {
                var dp = _datapackages.Get(hash);
                if (dp is null)
                {
                    return NotFound();
                }

                

                return Ok($"https://{_endpoint}:{_port}/Marti/sync/content?hash={hash}");
            }
            catch(Exception e)
            {
                return NotFound();
            }
            
        }

        [Route("/Marti/TracksKML")]
        [HttpPost]
        public string TracksKml()
        {
            // TODO: Implement TracksKML (/Marti/TracksKML)
            return "";
        }

        [Route("/Marti/ExportMissionKML")]
        [HttpPost]
        public string MissionKml()
        {
            // TODO: Implement this MissionKml (/Marti/ExportMissionKML)
            return "";
        }

        [Route("/Marti/vcm")]
        [HttpPost]
        public string UploadVideo()
        {
            // TODO: Implement this UploadVideo (/Marti/vcm)
            return "";
        }

        [Route("/Marti/vcm")]
        [HttpGet]
        public string ListVideos()
        {
            // TODO: Implement this ListVideos (/Marti/vcm)
            return "";
        }
    }
}