using dpp.cot;
using NetCoreServer;
using Serilog;
using System;
using System.Net.Sockets;

namespace dpp.opentakrouter
{
    public class TakWsSession : WsSession
    {
        private readonly IRouter _router;
        public TakWsSession(TakWsServer server) : base(server)
        {
            _router = server.Router;
        }
        public override void OnWsConnected(HttpRequest request)
        {
            Log.Information($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} state=connected");
        }

        public override void OnWsDisconnected()
        {
            Log.Information($"server=ws session={Id} state=disconnected");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                var msg = Message.Parse(buffer, (int)offset, (int)size);

                Log.Information($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} event=cot uid={msg.Event.Uid} type={msg.Event.Type}");
                _router.Send(msg.Event, buffer);
            }
            catch (Exception e)
            {
                Log.Error(e, $"server=ws endpoint={Socket.RemoteEndPoint} session={Id} type=unknown error=true forwarded=false");
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} error={error}");
        }
    }
}
