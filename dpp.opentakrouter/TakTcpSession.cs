using dpp.cot;
using NetCoreServer;
using Serilog;
using System;
using System.Net.Sockets;

namespace dpp.opentakrouter
{
    public class TakTcpSession : TcpSession
    {
        private readonly IRouter _router;
        public TakTcpSession(TakTcpServer server) : base(server)
        {
            _router = server.Router;
        }
        protected override void OnConnected()
        {
            Log.Information($"server=tak-tcp endpoint={Socket.RemoteEndPoint} session={Id} state=connected");
        }

        protected override void OnDisconnected()
        {
            Log.Information($"server=tak-tcp session={Id} state=disconnected");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                var msg = Message.Parse(buffer, (int)offset, (int)size);
                Log.Information($"server=tak-tcp endpoint={Socket.RemoteEndPoint} session={Id} event=cot uid={msg.Event.Uid} type={msg.Event.Type}");
                if (msg.Event.IsA(CotPredicates.t_ping))
                {
                    SendAsync(Event.Pong(msg.Event).ToXmlString());
                    return;
                }

                _router.Send(msg.Event, buffer);
            }
            catch (Exception e)
            {
                Log.Error(e, $"server=tak-tcp endpoint={Socket.RemoteEndPoint} session={Id} type=unknown error=true forwarded=false");
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=tak-tcp endpoint={Socket.RemoteEndPoint} session={Id} error={error}");
        }
    }
}
