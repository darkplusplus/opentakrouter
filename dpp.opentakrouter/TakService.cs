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
                var tcpServerConfig = configuration.GetSection("server:tak:tcp").Get<TakServerConfig>();
                if (tcpServerConfig is not null && tcpServerConfig.Enabled)
                {
                    _tcpServer = new TakTcpServer(
                        IPAddress.Any,
                        tcpServerConfig.Port,
                        router: router);
                    _tcpServer.Start();
                    Log.Information("server=tak-tcp state=started");
                }
                else
                {
                    Log.Information("server=tak-tcp state=skipped");
                }

                var tlsServerConfig = configuration.GetSection("server:tak:tls").Get<TakServerConfig>();
                if (tlsServerConfig is not null && tlsServerConfig.Enabled)
                {
                    var sslContext = new SslContext(SslProtocols.Tls12, new X509Certificate(
                        tlsServerConfig.Cert,
                        tlsServerConfig.Passphrase)
                    );

                    _tlsServer = new TakTlsServer(
                        sslContext,
                        IPAddress.Any,
                        tlsServerConfig.Port,
                        router: router);
                    _tlsServer.Start();
                    Log.Information("server=tak-ssl state=started");
                }
                else
                {
                    Log.Information("server=tak-ssl state=skipped");
                }

                var websocketConfig = configuration.GetSection("server:websockets").Get<WebConfig>();
                if (websocketConfig is not null && websocketConfig.Enabled)
                {
                    var port = websocketConfig.Port ?? 5500;
                    if (websocketConfig.Ssl)
                    {
                        var sslContext = new SslContext(SslProtocols.Tls12, new X509Certificate(
                            websocketConfig.Cert,
                            websocketConfig.Passphrase)
                        );

                        _wssServer = new TakWssServer(sslContext, IPAddress.Any, port, router);
                        _wssServer.Start();
                        Log.Information("server=wss state=started");
                    }
                    else
                    {
                        _wsServer = new TakWsServer(IPAddress.Any, port, router);
                        _wsServer.Start();
                        Log.Information("server=ws state=started");
                    }
                }
                else
                {
                    Log.Information("server=ws state=skipped");
                    Log.Information("server=wss state=skipped");
                }

                var peerConfigs = configuration.GetSection("server:peers").Get<List<TakPeerConfig>>();
                if (peerConfigs is not null)
                {
                    foreach (var peerConfig in peerConfigs)
                    {
                        if (peerConfig.Ssl)
                        {
                            throw new NotImplementedException("Federation of SSL peers is not implemented yet");
                        }
                        else
                        {
                            try
                            {
                                var address = Dns.GetHostEntry(peerConfig.Address)
                                    .AddressList.First(addr => addr.AddressFamily == AddressFamily.InterNetwork)
                                    .ToString();

                                var mode = (TakTcpPeer.Mode)Enum.Parse(typeof(TakTcpPeer.Mode), peerConfig.Mode, true);
                                var client = new TakTcpPeer(
                                    peerConfig.Name,
                                    address,
                                    peerConfig.Port,
                                    router: router,
                                    mode: mode
                                );
                                _tcpClients.Add(client);
                            }
                            catch (Exception e)
                            {
                                Log.Error($"peer={peerConfig.Name} error={e}");
                                continue;
                            }
                        }
                    }

                    foreach (var client in _tcpClients)
                    {
                        client.Connect();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "state=error");
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
                _tcpServer.Stop();
            }

            if (_wssServer is not null)
            {
                _tlsServer.Stop();
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
    }
}