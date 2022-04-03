using dpp.cot;
using NetCoreServer;
using Serilog;
using System;
using System.Net.Sockets;

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
            Log.Information($"server=wss endpoint={Socket.RemoteEndPoint} session={Id} state=connected");
        }

        public override void OnWsDisconnected()
        {
            Log.Information($"server=wss session={Id} state=disconnected");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                var msg = Message.Parse(buffer, (int)offset, (int)size);

                Log.Information($"server=wss endpoint={Socket.RemoteEndPoint} session={Id} event=cot uid={msg.Event.Uid} type={msg.Event.Type}");
                _router.Send(msg.Event, buffer);
            }
            catch (Exception e)
            {
                Log.Error(e, $"server=wss endpoint={Socket.RemoteEndPoint} session={Id} type=unknown error=true forwarded=false");
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=wss endpoint={Socket.RemoteEndPoint} session={Id} error={error}");
        }
    }
}
