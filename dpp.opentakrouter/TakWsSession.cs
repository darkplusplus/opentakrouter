using NetCoreServer;
using Serilog;
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
            foreach (var evt in _router.GetActiveEvents())
            {
                SendTextAsync(UiEventMessage.Serialize(evt));
            }
        }

        public override void OnWsDisconnected()
        {
            Log.Information($"server=ws session={Id} state=disconnected");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            Log.Debug($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} state=ignored direction=inbound");
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} error=true message=\"{error}\"");
        }
    }
}
