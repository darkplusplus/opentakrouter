using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using dpp.cot;
using NetCoreServer;
using Serilog;

namespace dpp.opentakrouter
{
    public class TakWssSession : WssSession
    {
        private readonly IRouter _router;
        public TakWssSession(TakWssServer server) : base(server)
        {
            _router = server.Router;
        }
        public override void OnWsConnected(HttpRequest request)
        {
            Log.Information($"id={Id} endpoint={Socket.RemoteEndPoint} state=connected");
        }

        public override void OnWsDisconnected()
        {
            Log.Information($"id={Id} state=disconnected");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                var msgstr = Encoding.UTF8.GetString(buffer);
                var msg = Message.Parse(buffer, (int)offset, (int)size);

                Log.Information($"id={Id} endpoint={Socket.RemoteEndPoint} event=cot type={msg.Event.Type}");
                _router.Send(msg.Event);
            }
            catch (Exception e)
            {
                Log.Error(e, $"id={Id} endpoint={Socket.RemoteEndPoint} type=unknown error=true forwarded=false");
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"id={Id} error={error}");
        }
    }
}
