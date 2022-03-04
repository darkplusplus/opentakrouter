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
        private readonly IConfiguration configuration;
        public TakService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var router = new Router();

            try
            {
                if (bool.Parse(configuration["server:tcp:enabled"]))
                {
                    var port = int.Parse(configuration["server:tcp:port"]);
                    _tcpServer = new TakTcpServer(IPAddress.Any, port, router);
                    _tcpServer.Start();
                    Log.Information("server=tcp state=started");
                }
                if (bool.Parse(configuration["server:tls:enabled"]))
                {
                    var cert = configuration["server:tls:server:cert"];
                    var passphrase = configuration["server:tls:server:passphrase"];
                    var sslContext = new SslContext(SslProtocols.Tls, new X509Certificate(cert, passphrase));

                    var port = int.Parse(configuration["server:tls:port"]);
                    _tlsServer = new TakTlsServer(sslContext, IPAddress.Any, port, router);
                    _tlsServer.Start();
                    Log.Information("server=tls state=started");
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