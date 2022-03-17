using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using NetCoreServer;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace dpp.opentakrouter
{
    public class TakService : IHostedService, IDisposable
    {
        private TakTcpServer _tcpServer = null;
        private TakTlsServer _tlsServer = null;
        private TakWsServer _wsServer = null;
        private readonly IRouter router;
        private readonly IConfiguration configuration;
        public TakService(IConfiguration configuration, IRouter router)
        {
            this.configuration = configuration;
            this.router = router;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (bool.Parse(configuration["server:tcp:enabled"]))
                {
                    var port = int.Parse(configuration["server:tcp:port"]);
                    _tcpServer = new TakTcpServer(IPAddress.Any, port, router);
                    _tcpServer.Start();
                    Log.Information("server=tcp state=started");
                }
                if (bool.Parse(configuration["server:ssl:enabled"]))
                {
                    var cert = configuration["server:ssl:server:cert"];
                    var passphrase = configuration["server:ssl:server:passphrase"];
                    var sslContext = new SslContext(SslProtocols.Tls, new X509Certificate(cert, passphrase));

                    var port = int.Parse(configuration["server:ssl:port"]);
                    _tlsServer = new TakTlsServer(sslContext, IPAddress.Any, port, router);
                    _tlsServer.Start();
                    Log.Information("server=ssl state=started");
                }
                if (bool.Parse(configuration["server:ws:enabled"]))
                {
                    var port = int.Parse(configuration["server:ws:port"] ?? "5003");
                    _wsServer = new TakWsServer(IPAddress.Any, port, router);
                    _wsServer.Start();
                    Log.Information("server=ws state=started");
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
            Log.Information("state=shutdown");
            if (_tcpServer is not null) _tcpServer.Stop();
            if (_tlsServer is not null) _tlsServer.Stop();

            return Task.CompletedTask;
        }

        public void Dispose() => GC.SuppressFinalize(this);
    }
}