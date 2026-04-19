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
        private readonly TakConnectionProtocol _protocol;
        private const string _component = "tak-tcp";
        public TakTcpSession(TakTcpServer server) : base(server)
        {
            _router = server.Router;
            _protocol = new TakConnectionProtocol(TakConnectionRole.Server, server.ProtocolPreference);
            _router.RaiseRoutedEvent += OnRoutedEvent;
        }
        protected override void OnConnected()
        {
            Log.Information($"server=tak-tcp endpoint={Socket.RemoteEndPoint} session={Id} state=connected");
            _protocol.Reset();
            foreach (var data in _protocol.GetInitialMessages())
            {
                SendAsync(data);
            }
            foreach (var evt in _router.GetActiveEvents())
            {
                SendAsync(_protocol.Serialize(CotMessageEnvelope.FromEvent(evt)));
            }
        }

        protected override void OnDisconnected()
        {
            _router.RaiseRoutedEvent -= OnRoutedEvent;
            Log.Information($"server=tak-tcp session={Id} state=disconnected");
        }
        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                foreach (var result in _protocol.Read(buffer, (int)offset, (int)size, $"server:{_component}:{Id}"))
                {
                    try
                    {
                        var evt = result.Envelope.Event;
                        Log.Information($"server={_component} endpoint={Socket.RemoteEndPoint} session={Id} event=cot uid={evt.Uid} type={evt.Type}");
                        if (result.ControlResponse != null)
                        {
                            SendAsync(result.ControlResponse);
                        }

                        if (evt.IsA(CotPredicates.t_ping))
                        {
                            SendAsync(_protocol.Serialize(CotMessageEnvelope.FromEvent(Event.Pong(evt))));
                            return;
                        }

                        _router.Route(result.Envelope);
                    }
                    catch (OverflowException)
                    {
                        Log.Error($"server={_component} endpoint={Socket.RemoteEndPoint} session={Id} type=unknown error=true forwarded=false message=\"Overflow error. Receiving too much data.\"");

                        // TODO: no real backoff control. kill connection?
                    }
                    catch (Exception e)
                    {
                        Log.Error($"server={_component} endpoint={Socket.RemoteEndPoint} session={Id} type=unknown error=true forwarded=false message=\"{e.Message}\"");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"server={_component} session={Id} type=unknown error=true forwarded=false message=\"{e.Message}\"");
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=tak-tcp endpoint={Socket.RemoteEndPoint} session={Id} error=true message=\"{error}\"");
        }

        private void OnRoutedEvent(object sender, RoutedEventArgs e)
        {
            var destinationId = $"server:{_component}:{Id}";
            if (e.Envelope.SourceId == destinationId)
            {
                return;
            }

            if (!_router.ShouldRouteTo(e.Envelope, destinationId))
            {
                return;
            }

            SendAsync(_protocol.Serialize(e.Envelope));
        }
    }
}
