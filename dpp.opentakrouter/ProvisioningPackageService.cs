using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace dpp.opentakrouter
{
    public class ProvisioningPackageService
    {
        private readonly IConfiguration _configuration;

        public ProvisioningPackageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public GeneratedProvisioningPackage Generate()
        {
            var endpoint = _configuration.GetValue("server:public_endpoint", "").Trim();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("server:public_endpoint must be set to generate a provisioning package.");
            }

            var takPort = ResolveTakTlsPort();
            var options = _configuration.GetSection("server:provisioning").Get<ProvisioningOptions>() ?? new ProvisioningOptions();
            var trustStore = LoadTrustStore(options);
            var clientCertificate = LoadClientCertificate(options);
            var packageId = Guid.NewGuid().ToString();
            var packageBaseName = SanitizeFileName(options.PackageName);
            var fileName = $"{packageBaseName}.zip";
            var connectString = $"{endpoint}:{takPort}:ssl";
            var manifestName = fileName;
            var atakConfigPref = BuildAtakConfigPref(connectString, options);
            var itakConfigPref = BuildItakConfigPref(connectString, options);
            var packageBytes = BuildZip(
                BuildManifestXml(packageId, manifestName, options),
                atakConfigPref,
                itakConfigPref,
                trustStore,
                clientCertificate);
            var hash = ComputeSha256(packageBytes);

            return new GeneratedProvisioningPackage(
                fileName,
                packageBaseName,
                hash,
                BuildProvisioningDownloadUrl(endpoint, options),
                packageBytes);
        }

        public string GetItakQrPayload()
        {
            var endpoint = _configuration.GetValue("server:public_endpoint", "").Trim();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("server:public_endpoint must be set to generate an iTAK QR payload.");
            }

            var options = _configuration.GetSection("server:provisioning").Get<ProvisioningOptions>() ?? new ProvisioningOptions();
            var takPort = ResolveTakTlsPort();
            return $"{options.ServerDescription},{endpoint},{takPort},ssl";
        }

        private int ResolveTakTlsPort()
        {
            var links = _configuration.GetSection("server:links").Get<TakLinkConfig[]>() ?? Array.Empty<TakLinkConfig>();
            var tlsListener = links.FirstOrDefault(link =>
                link.Enabled &&
                string.Equals(link.Role, "listen", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(link.Transport, "tls", StringComparison.OrdinalIgnoreCase) &&
                link.Port > 0);

            if (tlsListener is not null)
            {
                return tlsListener.Port;
            }

            return _configuration.GetValue("server:tak:tls:port", 8089);
        }

        private X509Certificate2 LoadServerCertificate()
        {
            var certPath = _configuration.GetValue("server:api:cert", "");
            var keyPath = _configuration.GetValue("server:api:key", "");
            var passphrase = _configuration.GetValue("server:api:passphrase", "");

            if (string.IsNullOrWhiteSpace(certPath))
            {
                throw new InvalidOperationException("server:api:cert must be set to generate a provisioning package.");
            }

            return CertificateOptions.Load(certPath, keyPath, passphrase);
        }

        private byte[] LoadTrustStore(ProvisioningOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.TrustStoreCertificate))
            {
                if (!File.Exists(options.TrustStoreCertificate))
                {
                    throw new FileNotFoundException($"Trust store certificate file was not found: {options.TrustStoreCertificate}", options.TrustStoreCertificate);
                }

                var extension = Path.GetExtension(options.TrustStoreCertificate);
                if (string.Equals(extension, ".p12", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".pfx", StringComparison.OrdinalIgnoreCase))
                {
                    var trustStore = X509CertificateLoader.LoadPkcs12Collection(
                        File.ReadAllBytes(options.TrustStoreCertificate),
                        options.TrustStorePassword,
                        X509KeyStorageFlags.DefaultKeySet,
                        Pkcs12LoaderLimits.Defaults);
                    return ExportTrustStore(ToTrustOnlyCollection(trustStore), options.TrustStorePassword);
                }

                var trustCertificates = LoadTrustCertificates(options.TrustStoreCertificate);
                return ExportTrustStore(trustCertificates, options.TrustStorePassword);
            }

            var serverCertificate = LoadServerCertificate();
            return ExportTrustStore(new X509Certificate2Collection(serverCertificate), options.TrustStorePassword);
        }

        private static byte[] LoadClientCertificate(ProvisioningOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ClientCertificate))
            {
                throw new InvalidOperationException("server:provisioning:clientCertificate must be set to generate a soft-cert package.");
            }

            if (!File.Exists(options.ClientCertificate))
            {
                throw new FileNotFoundException($"Client certificate file was not found: {options.ClientCertificate}", options.ClientCertificate);
            }

            return File.ReadAllBytes(options.ClientCertificate);
        }

        private string BuildProvisioningDownloadUrl(string endpoint, ProvisioningOptions options)
        {
            var scheme = ResolvePublicApiScheme(options);
            var port = ResolvePublicApiPort(options, scheme);

            if ((string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) && port == 443) ||
                (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) && port == 80))
            {
                return $"{scheme}://{endpoint}/Marti/api/provisioning/serverpackage";
            }

            return $"{scheme}://{endpoint}:{port}/Marti/api/provisioning/serverpackage";
        }

        private string ResolvePublicApiScheme(ProvisioningOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.PublicApiScheme))
            {
                return options.PublicApiScheme.Trim().ToLowerInvariant();
            }

            return _configuration.GetValue("server:api:ssl", true) ? "https" : "http";
        }

        private int ResolvePublicApiPort(ProvisioningOptions options, string scheme)
        {
            if (options.PublicApiPort > 0)
            {
                return options.PublicApiPort;
            }

            var defaultPort = string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) ? 8080 : 8443;
            return _configuration.GetValue("server:api:port", defaultPort);
        }

        private static X509Certificate2Collection LoadTrustCertificates(string path)
        {
            var certificates = new X509Certificate2Collection();
            certificates.ImportFromPemFile(path);

            if (certificates.Count > 0)
            {
                return certificates;
            }

            certificates.Add(X509CertificateLoader.LoadCertificateFromFile(path));
            return certificates;
        }

        private static byte[] ExportTrustStore(X509Certificate2Collection certificates, string password)
        {
            if (certificates.Count == 0)
            {
                throw new InvalidOperationException("At least one trust certificate is required to generate a provisioning package.");
            }

            return certificates.Export(X509ContentType.Pkcs12, password);
        }

        private static X509Certificate2Collection ToTrustOnlyCollection(X509Certificate2Collection certificates)
        {
            var trustOnly = new X509Certificate2Collection();
            foreach (var certificate in certificates)
            {
                trustOnly.Add(X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert)));
            }

            return trustOnly;
        }

        private static byte[] BuildZip(string manifestXml, string atakConfigPrefXml, string itakConfigPrefXml, byte[] trustStore, byte[] clientCertificate)
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Keep the ATAK/WinTAK mission-package layout while also placing
                // the files iTAK public guides expect at the root of the ZIP.
                WriteEntry(archive, "MANIFEST/manifest.xml", manifestXml);
                WriteEntry(archive, "MANIFEST.xml", manifestXml);
                WriteEntry(archive, "manifest.xml", manifestXml);
                WriteEntry(archive, "config.pref", itakConfigPrefXml);
                WriteEntry(archive, "preference.pref", itakConfigPrefXml);
                WriteEntry(archive, "tak-server.pref", itakConfigPrefXml);
                WriteEntry(archive, "server.p12", trustStore);
                WriteEntry(archive, "iphone.p12", clientCertificate);
                WriteEntry(archive, "cert/server.p12", trustStore);
                WriteEntry(archive, "cert/iphone.p12", clientCertificate);
                WriteEntry(archive, "certs/config.pref", atakConfigPrefXml);
                WriteEntry(archive, "certs/caCert.p12", trustStore);
                WriteEntry(archive, "certs/clientCert.p12", clientCertificate);
            }

            return stream.ToArray();
        }

        private static void WriteEntry(ZipArchive archive, string path, string content)
        {
            WriteEntry(archive, path, Encoding.ASCII.GetBytes(content));
        }

        private static void WriteEntry(ZipArchive archive, string path, byte[] content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
            using var output = entry.Open();
            output.Write(content, 0, content.Length);
        }

        private static string BuildManifestXml(string packageId, string packageName, ProvisioningOptions options)
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Encoding = Encoding.ASCII,
                Indent = true,
            };

            using var stringWriter = new EncodedStringWriter(Encoding.ASCII);
            using var writer = XmlWriter.Create(stringWriter, settings);
            writer.WriteStartElement("MissionPackageManifest");
            writer.WriteAttributeString("version", "2");

            writer.WriteStartElement("Configuration");
            WriteManifestParameter(writer, "uid", packageId);
            WriteManifestParameter(writer, "name", packageName);
            WriteManifestParameter(writer, "onReceiveDelete", options.OnReceiveDelete ? "true" : "false");
            writer.WriteEndElement();

            writer.WriteStartElement("Contents");
            WriteManifestContent(writer, "certs/config.pref");
            WriteManifestContent(writer, "certs/caCert.p12");
            WriteManifestContent(writer, "certs/clientCert.p12");
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.Flush();

            return stringWriter.ToString();
        }

        private static void WriteManifestParameter(XmlWriter writer, string name, string value)
        {
            writer.WriteStartElement("Parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value", value);
            writer.WriteEndElement();
        }

        private static void WriteManifestContent(XmlWriter writer, string zipEntry)
        {
            writer.WriteStartElement("Content");
            writer.WriteAttributeString("ignore", "false");
            writer.WriteAttributeString("zipEntry", zipEntry);
            writer.WriteEndElement();
        }

        private static string BuildAtakConfigPref(string connectString, ProvisioningOptions options)
        {
            return BuildConfigPref(
                connectString,
                caLocation: "certs/caCert.p12",
                certificateLocation: "certs/clientCert.p12",
                options);
        }

        private static string BuildItakConfigPref(string connectString, ProvisioningOptions options)
        {
            return BuildConfigPref(
                connectString,
                caLocation: "cert/server.p12",
                certificateLocation: "cert/iphone.p12",
                options);
        }

        private static string BuildConfigPref(string connectString, string caLocation, string certificateLocation, ProvisioningOptions options)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.ASCII,
                Indent = true,
                OmitXmlDeclaration = false,
            };

            using var stringWriter = new EncodedStringWriter(Encoding.ASCII);
            using var writer = XmlWriter.Create(stringWriter, settings);
            writer.WriteStartDocument(true);
            writer.WriteStartElement("preferences");

            writer.WriteStartElement("preference");
            writer.WriteAttributeString("version", "1");
            writer.WriteAttributeString("name", "cot_streams");
            WritePreferenceEntry(writer, "count", "class java.lang.Integer", "1");
            WritePreferenceEntry(writer, "description0", "class java.lang.String", options.ServerDescription);
            WritePreferenceEntry(writer, "enabled0", "class java.lang.Boolean", "true");
            WritePreferenceEntry(writer, "connectString0", "class java.lang.String", connectString);
            WritePreferenceEntry(writer, "caLocation0", "class java.lang.String", caLocation);
            WritePreferenceEntry(writer, "caPassword0", "class java.lang.String", options.TrustStorePassword);
            WritePreferenceEntry(writer, "certificateLocation0", "class java.lang.String", certificateLocation);
            WritePreferenceEntry(writer, "clientPassword0", "class java.lang.String", options.ClientCertificatePassword);
            writer.WriteEndElement();

            writer.WriteStartElement("preference");
            writer.WriteAttributeString("version", "1");
            writer.WriteAttributeString("name", "com.atakmap.app_preferences");
            WritePreferenceEntry(writer, "displayServerConnectionWidget", "class java.lang.Boolean", "true");
            WritePreferenceEntry(writer, "caLocation", "class java.lang.String", caLocation);
            WritePreferenceEntry(writer, "caPassword", "class java.lang.String", options.TrustStorePassword);
            WritePreferenceEntry(writer, "clientPassword", "class java.lang.String", options.ClientCertificatePassword);
            WritePreferenceEntry(writer, "certificateLocation", "class java.lang.String", certificateLocation);
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();

            return stringWriter.ToString();
        }

        private static void WritePreferenceEntry(XmlWriter writer, string key, string entryClass, string value)
        {
            writer.WriteStartElement("entry");
            writer.WriteAttributeString("key", key);
            writer.WriteAttributeString("class", entryClass);
            writer.WriteString(value);
            writer.WriteEndElement();
        }

        private static string SanitizeFileName(string value)
        {
            var fileName = string.IsNullOrWhiteSpace(value) ? "OpenTAKRouter" : value.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalid, '-');
            }

            return fileName.Replace(' ', '_');
        }

        private static string ComputeSha256(byte[] content)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(content);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    public sealed record GeneratedProvisioningPackage(
        string FileName,
        string DisplayName,
        string Hash,
        string DownloadUrl,
        byte[] Content);

    internal sealed class EncodedStringWriter : StringWriter
    {
        private readonly Encoding _encoding;

        public EncodedStringWriter(Encoding encoding)
        {
            _encoding = encoding;
        }

        public override Encoding Encoding => _encoding;
    }
}
