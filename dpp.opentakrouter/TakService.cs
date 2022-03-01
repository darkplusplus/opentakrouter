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

namespace dpp.opentakrouter
{
    public class TakService : IHostedService, IDisposable
    {
        private TakServer _server = null;
        private IConfiguration configuration;
        public TakService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (bool.Parse(configuration["server:tcp:enabled"]))
                {
                    var port = int.Parse(configuration["server:tcp:port"]);
                    _server = new TakServer(IPAddress.Any, port);
                    _server.Start();

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
            if (_server is not null)
            {
                _server.Stop();
            }
            
            return Task.CompletedTask;
        }

        public void Dispose() => GC.SuppressFinalize(this);
    }
}