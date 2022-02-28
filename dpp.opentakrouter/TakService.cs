using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using NetCoreServer;
using System.Net;

namespace dpp.opentakrouter
{
    public class TakService : IHostedService, IDisposable
    {
        private TakServer _server = null;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server = new TakServer(IPAddress.Any, 8888);
            _server.Start();
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.Start();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}