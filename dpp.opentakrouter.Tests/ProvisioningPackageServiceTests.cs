using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace dpp.opentakrouter.Tests
{
    public class ProvisioningPackageServiceTests
    {
        [Fact]
        public void GenerateBuildsSoftCertPackageWithManifestAndClientCertificate()
        {
            using var certificateFixture = TestCertificateFixture.Create();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["server:public_endpoint"] = "tak.example.com",
                    ["server:api:port"] = "8443",
                    ["server:api:cert"] = certificateFixture.CertificatePath,
                    ["server:api:passphrase"] = certificateFixture.Passphrase,
                    ["server:provisioning:packageName"] = "OTR Enrollment",
                    ["server:provisioning:serverDescription"] = "OpenTAKRouter",
                    ["server:provisioning:trustStoreCertificate"] = certificateFixture.TrustCertificatePath,
                    ["server:provisioning:trustStorePassword"] = "trust-pass",
                    ["server:provisioning:clientCertificate"] = certificateFixture.ClientCertificatePath,
                    ["server:provisioning:clientCertificatePassword"] = "client-pass",
                    ["server:links:0:enabled"] = "true",
                    ["server:links:0:role"] = "listen",
                    ["server:links:0:transport"] = "tls",
                    ["server:links:0:port"] = "8089",
                })
                .Build();

            var package = new ProvisioningPackageService(configuration).Generate();

            Assert.Equal("OTR_Enrollment.zip", package.FileName);
            Assert.Equal("https://tak.example.com:8443/Marti/api/provisioning/serverpackage", package.DownloadUrl);
            Assert.False(string.IsNullOrWhiteSpace(package.Hash));

            using var stream = new MemoryStream(package.Content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var manifest = ReadEntry(archive, "MANIFEST/manifest.xml");
            var rootManifest = ReadEntry(archive, "manifest.xml");
            var upperManifest = ReadEntry(archive, "MANIFEST.xml");
            var rootPreferences = ReadEntry(archive, "config.pref");
            var itakPreferences = ReadEntry(archive, "tak-server.pref");
            var atakPreferences = ReadEntry(archive, "certs/config.pref");
            var trustStore = archive.GetEntry("certs/caCert.p12");
            var clientCert = archive.GetEntry("certs/clientCert.p12");
            var rootTrustStore = archive.GetEntry("server.p12");
            var rootClientCert = archive.GetEntry("iphone.p12");

            Assert.Contains("MissionPackageManifest", manifest);
            Assert.Contains("OTR_Enrollment.zip", manifest);
            Assert.Contains("certs/config.pref", manifest);
            Assert.Contains("certs/caCert.p12", manifest);
            Assert.Contains("certs/clientCert.p12", manifest);
            Assert.Equal(manifest, rootManifest);
            Assert.Equal(manifest, upperManifest);

            Assert.Contains("tak.example.com:8089:ssl", atakPreferences);
            Assert.Contains("caLocation0", atakPreferences);
            Assert.Contains("caPassword0", atakPreferences);
            Assert.Contains("certificateLocation0", atakPreferences);
            Assert.Contains("clientPassword0", atakPreferences);
            Assert.Contains("certs/caCert.p12", atakPreferences);
            Assert.Contains("certs/clientCert.p12", atakPreferences);
            Assert.Contains("client-pass", atakPreferences);
            Assert.DoesNotContain("enrollForCertificateWithTrust0", atakPreferences);
            Assert.DoesNotContain("useAuth0", atakPreferences);
            Assert.DoesNotContain("locationCallsign", atakPreferences);
            Assert.DoesNotContain("locationTeam", atakPreferences);
            Assert.DoesNotContain("atakRoleType", atakPreferences);

            Assert.Contains("tak.example.com:8089:ssl", rootPreferences);
            Assert.Contains("cert/server.p12", rootPreferences);
            Assert.Contains("cert/iphone.p12", rootPreferences);
            Assert.Equal(rootPreferences, itakPreferences);

            Assert.NotNull(trustStore);
            Assert.True(trustStore.Length > 0);
            Assert.NotNull(clientCert);
            Assert.True(clientCert.Length > 0);
            Assert.NotNull(rootTrustStore);
            Assert.NotNull(rootClientCert);
        }

        [Fact]
        public void GeneratePrefersProvisioningPublicApiOverrides()
        {
            using var certificateFixture = TestCertificateFixture.Create();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["server:public_endpoint"] = "tak.example.com",
                    ["server:api:port"] = "8443",
                    ["server:api:ssl"] = "true",
                    ["server:api:cert"] = certificateFixture.CertificatePath,
                    ["server:api:passphrase"] = certificateFixture.Passphrase,
                    ["server:provisioning:publicApiScheme"] = "https",
                    ["server:provisioning:publicApiPort"] = "443",
                    ["server:provisioning:trustStoreCertificate"] = certificateFixture.TrustCertificatePath,
                    ["server:provisioning:clientCertificate"] = certificateFixture.ClientCertificatePath,
                    ["server:links:0:enabled"] = "true",
                    ["server:links:0:role"] = "listen",
                    ["server:links:0:transport"] = "tls",
                    ["server:links:0:port"] = "8089",
                })
                .Build();

            var package = new ProvisioningPackageService(configuration).Generate();

            Assert.Equal("https://tak.example.com/Marti/api/provisioning/serverpackage", package.DownloadUrl);
        }

        [Fact]
        public void GeneratePreservesPkcs12TrustChainWhenProvided()
        {
            using var certificateFixture = TestCertificateFixture.CreateWithPkcs12TrustBundle();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["server:public_endpoint"] = "tak.example.com",
                    ["server:api:cert"] = certificateFixture.CertificatePath,
                    ["server:api:passphrase"] = certificateFixture.Passphrase,
                    ["server:provisioning:trustStoreCertificate"] = certificateFixture.TrustCertificatePath,
                    ["server:provisioning:trustStorePassword"] = "trust-pass",
                    ["server:provisioning:clientCertificate"] = certificateFixture.ClientCertificatePath,
                    ["server:links:0:enabled"] = "true",
                    ["server:links:0:role"] = "listen",
                    ["server:links:0:transport"] = "tls",
                    ["server:links:0:port"] = "8089",
                })
                .Build();

            var package = new ProvisioningPackageService(configuration).Generate();

            using var stream = new MemoryStream(package.Content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            using var trustStream = archive.GetEntry("certs/caCert.p12")?.Open();
            using var trustMemory = new MemoryStream();
            trustStream?.CopyTo(trustMemory);

            var certificates = X509CertificateLoader.LoadPkcs12Collection(
                trustMemory.ToArray(),
                "trust-pass",
                X509KeyStorageFlags.DefaultKeySet,
                Pkcs12LoaderLimits.Defaults);

            Assert.Equal(2, certificates.Count);
        }

        [Fact]
        public void GenerateRequiresPublicEndpoint()
        {
            using var certificateFixture = TestCertificateFixture.Create();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["server:api:cert"] = certificateFixture.CertificatePath,
                    ["server:api:passphrase"] = certificateFixture.Passphrase,
                    ["server:provisioning:trustStoreCertificate"] = certificateFixture.TrustCertificatePath,
                    ["server:provisioning:clientCertificate"] = certificateFixture.ClientCertificatePath,
                })
                .Build();

            var service = new ProvisioningPackageService(configuration);
            var exception = Assert.Throws<InvalidOperationException>(() => service.Generate());

            Assert.Contains("server:public_endpoint", exception.Message);
        }

        [Fact]
        public void GenerateRequiresClientCertificate()
        {
            using var certificateFixture = TestCertificateFixture.Create();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["server:public_endpoint"] = "tak.example.com",
                    ["server:api:cert"] = certificateFixture.CertificatePath,
                    ["server:api:passphrase"] = certificateFixture.Passphrase,
                    ["server:provisioning:trustStoreCertificate"] = certificateFixture.TrustCertificatePath,
                })
                .Build();

            var service = new ProvisioningPackageService(configuration);
            var exception = Assert.Throws<InvalidOperationException>(() => service.Generate());

            Assert.Contains("clientCertificate", exception.Message);
        }

        [Fact]
        public void GetItakQrPayloadReturnsQuickConnectCsv()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["server:public_endpoint"] = "tak.example.com",
                    ["server:provisioning:serverDescription"] = "OpenTAKRouter",
                    ["server:links:0:enabled"] = "true",
                    ["server:links:0:role"] = "listen",
                    ["server:links:0:transport"] = "tls",
                    ["server:links:0:port"] = "8089",
                })
                .Build();

            var payload = new ProvisioningPackageService(configuration).GetItakQrPayload();

            Assert.Equal("OpenTAKRouter,tak.example.com,8089,ssl", payload);
        }

        private static string ReadEntry(ZipArchive archive, string path)
        {
            using var stream = archive.GetEntry(path)?.Open();
            using var reader = new StreamReader(stream ?? throw new InvalidOperationException($"Missing entry: {path}"), Encoding.ASCII);
            return reader.ReadToEnd();
        }

        private sealed class TestCertificateFixture : IDisposable
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
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(365));

                var request = new CertificateRequest(
                    "CN=tak.example.com",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
                using var certificate = request.Create(
                    caCertificate,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(30),
                    Guid.NewGuid().ToByteArray());

                using var clientKey = RSA.Create(2048);
                var clientRequest = new CertificateRequest(
                    "CN=client.tak.example.com",
                    clientKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                clientRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                clientRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(clientRequest.PublicKey, false));
                using var clientCertificate = clientRequest.Create(
                    caCertificate,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(30),
                    Guid.NewGuid().ToByteArray());
                var password = "change-me";
                var path = Path.Combine(Path.GetTempPath(), $"otr-provisioning-{Guid.NewGuid():N}.p12");
                var trustPath = Path.Combine(Path.GetTempPath(), $"otr-ca-{Guid.NewGuid():N}.crt");
                var clientPath = Path.Combine(Path.GetTempPath(), $"otr-client-{Guid.NewGuid():N}.p12");
                File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
                File.WriteAllBytes(trustPath, caCertificate.Export(X509ContentType.Cert));
                File.WriteAllBytes(clientPath, clientCertificate.Export(X509ContentType.Pkcs12, password));
                return new TestCertificateFixture(path, trustPath, clientPath, password);
            }

            public static TestCertificateFixture CreateWithPkcs12TrustBundle()
            {
                using var rsa = RSA.Create(2048);
                using var rootKey = RSA.Create(2048);
                var rootRequest = new CertificateRequest(
                    "CN=OpenTAKRouter Test Root",
                    rootKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 1, true));
                rootRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, false));
                using var rootCertificate = rootRequest.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(365));

                using var intermediateKey = RSA.Create(2048);
                var intermediateRequest = new CertificateRequest(
                    "CN=OpenTAKRouter Test Intermediate",
                    intermediateKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                intermediateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
                intermediateRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(intermediateRequest.PublicKey, false));
                using var intermediateCertificateWithoutKey = intermediateRequest.Create(
                    rootCertificate,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(180),
                    Guid.NewGuid().ToByteArray());
                using var intermediateCertificate = intermediateCertificateWithoutKey.CopyWithPrivateKey(intermediateKey);

                var request = new CertificateRequest(
                    "CN=tak.example.com",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
                using var certificate = request.Create(
                    intermediateCertificate,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(30),
                    Guid.NewGuid().ToByteArray());

                using var clientKey = RSA.Create(2048);
                var clientRequest = new CertificateRequest(
                    "CN=client.tak.example.com",
                    clientKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                clientRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                clientRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(clientRequest.PublicKey, false));
                using var clientCertificate = clientRequest.Create(
                    intermediateCertificate,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(30),
                    Guid.NewGuid().ToByteArray());

                var password = "change-me";
                var path = Path.Combine(Path.GetTempPath(), $"otr-provisioning-{Guid.NewGuid():N}.p12");
                var trustPath = Path.Combine(Path.GetTempPath(), $"otr-ca-bundle-{Guid.NewGuid():N}.p12");
                var clientPath = Path.Combine(Path.GetTempPath(), $"otr-client-{Guid.NewGuid():N}.p12");
                var trustBundle = new X509Certificate2Collection();
                trustBundle.Add(intermediateCertificate);
                trustBundle.Add(rootCertificate);
                File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
                File.WriteAllBytes(
                    trustPath,
                    trustBundle.Export(X509ContentType.Pkcs12, "trust-pass"));
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
