using NetCoreServer;
using Serilog;
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
            foreach (var evt in _router.GetActiveEvents())
            {
                SendTextAsync(UiEventMessage.Serialize(evt));
            }
        }

        public override void OnWsDisconnected()
        {
            Log.Information($"server=wss session={Id} state=disconnected");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            Log.Debug($"server=wss endpoint={Socket.RemoteEndPoint} session={Id} state=ignored direction=inbound");
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=wss endpoint={Socket.RemoteEndPoint} session={Id} error=true message=\"{error}\"");
        }
    }
}
