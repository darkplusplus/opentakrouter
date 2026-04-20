using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace dpp.opentakrouter
{
    public static class CertificateOptions
    {
        public static X509Certificate2 Load(string certPath, string keyPath = "", string passphrase = "")
        {
            if (string.IsNullOrWhiteSpace(certPath))
            {
                throw new InvalidOperationException("A certificate path is required when TLS is enabled.");
            }

            if (!File.Exists(certPath))
            {
                throw new FileNotFoundException($"Certificate file was not found: {certPath}", certPath);
            }

            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                if (!File.Exists(keyPath))
                {
                    throw new FileNotFoundException($"Certificate key file was not found: {keyPath}", keyPath);
                }

                return X509Certificate2.CreateFromPemFile(certPath, keyPath);
            }

            return X509CertificateLoader.LoadPkcs12FromFile(
                certPath,
                passphrase,
                X509KeyStorageFlags.DefaultKeySet,
                Pkcs12LoaderLimits.Defaults);
        }
    }
}
