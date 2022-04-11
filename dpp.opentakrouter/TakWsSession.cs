using dpp.cot;
using NetCoreServer;
using Serilog;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

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
                SendTextAsync(evt.ToXmlString());
            }
        }

        public override void OnWsDisconnected()
        {
            Log.Information($"server=ws session={Id} state=disconnected");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                var data = Encoding.UTF8.GetString(buffer);

                foreach (Match match in Regex.Matches(data, @"<event.+?\/event>"))
                {
                    try
                    {
                        var evt = Event.Parse(match.Value);
                        Log.Information($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} event=cot uid={evt.Uid} type={evt.Type}");
                        _router.Send(evt, null);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} type=unknown error=true forwarded=false message=\"{e.Message}\"");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} type=unknown error=true forwarded=false message=\"{e.Message}\"");
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=ws endpoint={Socket.RemoteEndPoint} session={Id} error=true message=\"{error}\"");
        }
    }
}
