using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetCoreServer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace dpp.opentakrouter
{
    public class TakService : IHostedService, IDisposable
    {
        private TakTcpServer _tcpServer = null;
        private TakTlsServer _tlsServer = null;
        private TakWsServer _wsServer = null;
        private TakWssServer _wssServer = null;
        private readonly List<TakTcpPeer> _tcpClients;
        private readonly IRouter router;
        private readonly IConfiguration configuration;
        public TakService(IConfiguration configuration, IRouter router)
        {
            this.configuration = configuration;
            this.router = router;
            _tcpClients = new List<TakTcpPeer>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var link in GetTakLinks())
                {
                    StartTakLink(link);
                }

                var websocketConfig = configuration.GetSection("server:websockets").Get<WebConfig>();
                if (websocketConfig is not null && websocketConfig.Enabled)
                {
                    var port = websocketConfig.Port ?? 5500;
                    if (websocketConfig.Ssl)
                    {
                        var sslContext = new SslContext(
                            SslProtocols.Tls12,
                            LoadPkcs12Certificate(websocketConfig.Cert, websocketConfig.Passphrase));

                        _wssServer = new TakWssServer(sslContext, IPAddress.Any, port, router);
                        _wssServer.Start();
                        Log.Information($"server=wss state=started port={port}");
                    }
                    else
                    {
                        _wsServer = new TakWsServer(IPAddress.Any, port, router);
                        _wsServer.Start();
                        Log.Information($"server=ws state=started port={port}");
                    }
                }
                else
                {
                    Log.Information("server=ws state=skipped");
                    Log.Information("server=wss state=skipped");
                }
            }
            catch (Exception e)
            {
                Log.Error($"state=error error=true message=\"{e.Message}\"");
                System.Environment.Exit(2);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("state=stopping");
            if (_tcpServer is not null)
            {
                _tcpServer.Stop();
            }

            if (_tlsServer is not null)
            {
                _tlsServer.Stop();
            }

            if (_wsServer is not null)
            {
                _wsServer.Stop();
            }

            if (_wssServer is not null)
            {
                _wssServer.Stop();
            }

            if (_tcpClients is not null)
            {
                foreach (var client in _tcpClients)
                {
                    client.DisconnectAndStop();
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose() => GC.SuppressFinalize(this);

        private IEnumerable<TakLinkConfig> GetTakLinks()
        {
            var configuredLinks = configuration.GetSection("server:links").Get<List<TakLinkConfig>>();
            if ((configuredLinks != null) && (configuredLinks.Count > 0))
            {
                return configuredLinks.Where(link => link?.Enabled ?? false);
            }

            var legacyLinks = new List<TakLinkConfig>();
            var tcpServerConfig = configuration.GetSection("server:tak:tcp").Get<TakServerConfig>();
            if ((tcpServerConfig is not null) && tcpServerConfig.Enabled)
            {
                legacyLinks.Add(new TakLinkConfig
                {
                    Name = "tak-tcp",
                    Enabled = true,
                    Role = "listen",
                    Transport = "tcp",
                    Port = tcpServerConfig.Port,
                    Protocol = tcpServerConfig.Protocol,
                });
            }

            var tlsServerConfig = configuration.GetSection("server:tak:tls").Get<TakServerConfig>();
            if ((tlsServerConfig is not null) && tlsServerConfig.Enabled)
            {
                legacyLinks.Add(new TakLinkConfig
                {
                    Name = "tak-tls",
                    Enabled = true,
                    Role = "listen",
                    Transport = "tls",
                    Port = tlsServerConfig.Port,
                    Cert = tlsServerConfig.Cert,
                    Passphrase = tlsServerConfig.Passphrase,
                    Protocol = tlsServerConfig.Protocol,
                });
            }

            var peerConfigs = configuration.GetSection("server:peers").Get<List<TakPeerConfig>>();
            if (peerConfigs != null)
            {
                foreach (var peer in peerConfigs)
                {
                    legacyLinks.Add(new TakLinkConfig
                    {
                        Name = peer.Name,
                        Enabled = true,
                        Role = "connect",
                        Transport = peer.Ssl ? "tls" : "tcp",
                        Address = peer.Address,
                        Port = peer.Port,
                        Mode = peer.Mode,
                        Protocol = peer.Protocol,
                    });
                }
            }

            return legacyLinks;
        }

        private void StartTakLink(TakLinkConfig link)
        {
            var transport = (link.Transport ?? "tcp").ToLowerInvariant();
            var role = (link.Role ?? "listen").ToLowerInvariant();
            var linkName = string.IsNullOrWhiteSpace(link.Name) ? $"{role}-{transport}-{link.Port}" : link.Name;

            if (role == "listen")
            {
                if (transport == "tcp")
                {
                    _tcpServer = new TakTcpServer(
                        IPAddress.Any,
                        link.Port,
                        router: router,
                        protocolPreference: TakProtocolPreferences.Parse(link.Protocol));
                    _tcpServer.Start();
                    Log.Information($"link={linkName} role=listen transport=tcp state=started port={link.Port}");
                    return;
                }

                if (transport == "tls")
                {
                    var sslContext = new SslContext(
                        SslProtocols.Tls12,
                        LoadPkcs12Certificate(link.Cert, link.Passphrase));
                    _tlsServer = new TakTlsServer(
                        sslContext,
                        IPAddress.Any,
                        link.Port,
                        router: router,
                        protocolPreference: TakProtocolPreferences.Parse(link.Protocol));
                    _tlsServer.Start();
                    Log.Information($"link={linkName} role=listen transport=tls state=started port={link.Port}");
                    return;
                }
            }

            if (role == "connect")
            {
                if (transport == "tls")
                {
                    Log.Error($"link={linkName} role=connect transport=tls error=true message=\"TLS federation connectors are not implemented yet\"");
                    return;
                }

                try
                {
                    var address = Dns.GetHostEntry(link.Address)
                        .AddressList.First(addr => addr.AddressFamily == AddressFamily.InterNetwork)
                        .ToString();

                    var mode = (TakTcpPeer.Mode)Enum.Parse(typeof(TakTcpPeer.Mode), link.Mode ?? "duplex", true);
                    var client = new TakTcpPeer(
                        linkName,
                        address,
                        link.Port,
                        router: router,
                        protocolPreference: TakProtocolPreferences.Parse(link.Protocol),
                        mode: mode
                    );
                    _tcpClients.Add(client);
                    client.Connect();
                    Log.Information($"link={linkName} role=connect transport=tcp state=started endpoint={address}:{link.Port}");
                }
                catch (Exception e)
                {
                    Log.Error($"link={linkName} role=connect transport={transport} error=true message=\"{e.Message}\"");
                }
            }
        }

        private static X509Certificate2 LoadPkcs12Certificate(string path, string password)
        {
            return X509CertificateLoader.LoadPkcs12FromFile(
                path,
                password,
                X509KeyStorageFlags.DefaultKeySet,
                Pkcs12LoaderLimits.Defaults);
        }
    }
}
