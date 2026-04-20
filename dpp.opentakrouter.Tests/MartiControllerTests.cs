using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

        [Fact]
        public void ProvisioningDatapackageReturnsZip()
        {
            using var certificateFixture = TestCertificateFixture.Create();
            var controller = CreateController(certificateFixture);

            var result = Assert.IsType<FileContentResult>(controller.GetProvisioningDatapackage());
            Assert.Equal("application/zip", result.ContentType);
            Assert.EndsWith(".zip", result.FileDownloadName);

            using var stream = new MemoryStream(result.FileContents);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            Assert.NotNull(archive.GetEntry("MANIFEST/manifest.xml"));
            Assert.NotNull(archive.GetEntry("MANIFEST.xml"));
            Assert.NotNull(archive.GetEntry("manifest.xml"));
            Assert.NotNull(archive.GetEntry("config.pref"));
            Assert.NotNull(archive.GetEntry("tak-server.pref"));
            Assert.NotNull(archive.GetEntry("server.p12"));
            Assert.NotNull(archive.GetEntry("iphone.p12"));
            Assert.NotNull(archive.GetEntry("certs/config.pref"));
            Assert.NotNull(archive.GetEntry("certs/caCert.p12"));
            Assert.NotNull(archive.GetEntry("certs/clientCert.p12"));
        }

        [Fact]
        public void ItakQrPayloadReturnsQuickConnectCsv()
        {
            var controller = CreateController();

            var result = Assert.IsType<OkObjectResult>(controller.GetItakQrPayload());
            var payload = Assert.IsType<Dictionary<string, object>>(result.Value);

            Assert.Equal("OpenTAKRouter,router.local,8089,ssl", payload["payload"]);
        }

        private static MartiController CreateController(TestCertificateFixture certificateFixture = null, FakeDataPackageRepository packages = null, FakeClientRepository clients = null)
        {
            var values = new Dictionary<string, string>
            {
                ["server:public_endpoint"] = "router.local",
                ["server:api:port"] = "8443",
                ["server:links:0:enabled"] = "true",
                ["server:links:0:role"] = "listen",
                ["server:links:0:transport"] = "tls",
                ["server:links:0:port"] = "8089",
            };

            if (certificateFixture is not null)
            {
                values["server:api:cert"] = certificateFixture.CertificatePath;
                values["server:api:passphrase"] = certificateFixture.Passphrase;
                values["server:provisioning:trustStoreCertificate"] = certificateFixture.TrustCertificatePath;
                values["server:provisioning:clientCertificate"] = certificateFixture.ClientCertificatePath;
                values["server:provisioning:clientCertificatePassword"] = certificateFixture.Passphrase;
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
            var provisioning = new ProvisioningPackageService(configuration);

            return new MartiController(
                NullLogger<MartiController>.Instance,
                configuration,
                packages ?? new FakeDataPackageRepository(new DataPackage()),
                clients ?? new FakeClientRepository(),
                provisioning);
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

        private sealed class TestCertificateFixture : System.IDisposable
        {
            public string CertificatePath { get; }
            public string TrustCertificatePath { get; }
            public string Passphrase { get; }
            public string ClientCertificatePath { get; }

            private TestCertificateFixture(string certificatePath, string trustCertificatePath, string clientCertificatePath, string passphrase)
            {
                CertificatePath = certificatePath;
                TrustCertificatePath = trustCertificatePath;
                ClientCertificatePath = clientCertificatePath;
                Passphrase = passphrase;
            }

            public static TestCertificateFixture Create()
            {
                using var rsa = RSA.Create(2048);
                using var trustKey = RSA.Create(2048);
                var caRequest = new CertificateRequest(
                    "CN=OpenTAKRouter Test CA",
                    trustKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
                caRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(caRequest.PublicKey, false));
                using var caCertificate = caRequest.CreateSelfSigned(
                    System.DateTimeOffset.UtcNow.AddDays(-1),
                    System.DateTimeOffset.UtcNow.AddDays(365));

                var request = new CertificateRequest(
                    "CN=router.local",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
                using var certificate = request.Create(
                    caCertificate,
                    System.DateTimeOffset.UtcNow.AddDays(-1),
                    System.DateTimeOffset.UtcNow.AddDays(30),
                    System.Guid.NewGuid().ToByteArray());

                using var clientKey = RSA.Create(2048);
                var clientRequest = new CertificateRequest(
                    "CN=router-client",
                    clientKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                clientRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                clientRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(clientRequest.PublicKey, false));
                using var clientCertificate = clientRequest.Create(
                    caCertificate,
                    System.DateTimeOffset.UtcNow.AddDays(-1),
                    System.DateTimeOffset.UtcNow.AddDays(30),
                    System.Guid.NewGuid().ToByteArray());
                var password = "change-me";
                var path = Path.Combine(Path.GetTempPath(), $"otr-test-{System.Guid.NewGuid():N}.p12");
                var trustPath = Path.Combine(Path.GetTempPath(), $"otr-test-ca-{System.Guid.NewGuid():N}.crt");
                var clientPath = Path.Combine(Path.GetTempPath(), $"otr-test-client-{System.Guid.NewGuid():N}.p12");
                File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
                File.WriteAllBytes(trustPath, caCertificate.Export(X509ContentType.Cert));
                File.WriteAllBytes(clientPath, clientCertificate.Export(X509ContentType.Pkcs12, password));
                return new TestCertificateFixture(path, trustPath, clientPath, password);
            }

            public void Dispose()
            {
                if (File.Exists(CertificatePath))
                {
                    File.Delete(CertificatePath);
                }
                if (File.Exists(TrustCertificatePath))
                {
                    File.Delete(TrustCertificatePath);
                }
                if (File.Exists(ClientCertificatePath))
                {
                    File.Delete(ClientCertificatePath);
                }
            }
        }
    }
}
