using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using dpp.opentakrouter.Controllers;
using dpp.opentakrouter.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace dpp.opentakrouter.Tests
{
    public class MartiControllerTests
    {
        [Fact]
        public void ClientEndpointsReturnsKnownClients()
        {
            var controller = CreateController(
                clients: new FakeClientRepository(new[]
                {
                    new Client
                    {
                        Uid = "ANDROID-1",
                        Callsign = "VIPER",
                        LastStatus = "Connected"
                    }
                }));

            var result = (Dictionary<string, object>)controller.ClientEndpoints();
            var data = (List<object>)result["data"];
            var entry = (Dictionary<string, object>)data[0];

            Assert.Single(data);
            Assert.Equal("ANDROID-1", entry["uid"]);
            Assert.Equal("VIPER", entry["callsign"]);
        }

        [Fact]
        public async Task UpdateDatapackageMetadataReadsBodyAndSetsVisibility()
        {
            var packages = new FakeDataPackageRepository(new DataPackage
            {
                Hash = "hash-1",
                UID = "pkg",
                MIMEType = "application/zip",
                IsPrivate = true
            });

            var controller = CreateController(packages: packages);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("public"));

            var result = await controller.UpdateDatapackageMetadata("hash-1");

            Assert.IsType<OkObjectResult>(result);
            Assert.False(packages.Package.IsPrivate);
        }

        private static MartiController CreateController(FakeDataPackageRepository packages = null, FakeClientRepository clients = null)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["server:public_endpoint"] = "router.local",
                    ["server:api:port"] = "8080",
                })
                .Build();

            return new MartiController(
                NullLogger<MartiController>.Instance,
                configuration,
                packages ?? new FakeDataPackageRepository(new DataPackage()),
                clients ?? new FakeClientRepository());
        }

        private sealed class FakeClientRepository : IClientRepository
        {
            private readonly List<Client> _clients;

            public FakeClientRepository(IEnumerable<Client> clients = null)
            {
                _clients = clients is null ? new List<Client>() : new List<Client>(clients);
            }

            public int Add(Client c) { _clients.Add(c); return 1; }
            public int Delete(string c) => 1;
            public Client Get(string callsign) => _clients.Find(client => client.Callsign == callsign);
            public IEnumerable<Client> Search(string query = "") => _clients;
            public int Update(Client c) => 1;
            public int Upsert(Client c) => 1;
        }

        private sealed class FakeDataPackageRepository : IDataPackageRepository
        {
            public DataPackage Package { get; private set; }

            public FakeDataPackageRepository(DataPackage package)
            {
                Package = package;
            }

            public int Add(DataPackage dp) { Package = dp; return 1; }
            public int Add(IFormFile file, string hash, string filename, string submissionUser = "Anonymous", string creatorUid = "Anonymous", string keywords = "missionpackage", string visibility = "private") => 1;
            public int Delete(string hash) => 1;
            public DataPackage Get(string hash) => Package;
            public IEnumerable<DataPackage> Search(string keywords = "") => new[] { Package };
            public int Update(DataPackage dp) { Package = dp; return 1; }
        }
    }
}
