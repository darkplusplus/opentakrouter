using dpp.cot;
using Serilog;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TcpClient = NetCoreServer.TcpClient;

namespace dpp.opentakrouter
{
    public class TakTcpPeer : TcpClient
    {
        public enum Mode
        {
            Receive,
            Transmit,
            Duplex
        }

        private readonly IRouter _router;
        private readonly Mode _clientMode;
        private readonly int _initialBackoff = 3000;
        private readonly int _maxBackoff = 300000;
        private int _backoff;
        private bool _stop;
        private readonly string _name;

        public TakTcpPeer(string name, string address, int port, IRouter router, Mode mode = Mode.Duplex, int minBackoff = 3000, int maxBackoff = 300000) : base(address, port)
        {
            _stop = false;
            _name = name;
            _clientMode = mode;
            _initialBackoff = minBackoff;
            _maxBackoff = maxBackoff;
            _backoff = _initialBackoff;

            _router = router;
            _router.RaiseRoutedEvent += OnRoutedEvent;
        }

        protected void OnRoutedEvent(object sender, RoutedEventArgs e)
        {
            if (_clientMode == Mode.Transmit || _clientMode == Mode.Duplex)
            {
                _ = Send(e.Data);
            }
        }

        protected override void OnConnecting()
        {
            Log.Information($"peer={_name} state=connecting");
        }

        protected override void OnConnected()
        {
            Log.Information($"peer={_name} state=connected");
            _backoff = _initialBackoff;

            foreach (var evt in _router.GetActiveEvents())
            {
                SendAsync(evt.ToXmlString());
            }
        }

        protected override void OnDisconnected()
        {
            Log.Information($"peer={_name} state=reconnecting backoff={_backoff}");
            Thread.Sleep(_backoff);
            _backoff = Math.Clamp((int)Math.Round(_backoff * Math.E), _initialBackoff, _maxBackoff);

            if (!_stop)
            {
                ConnectAsync();
            }
        }

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
            {
                Thread.Yield();
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"peer={_name} error=true message=\"{error}\"");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (_clientMode == Mode.Receive || _clientMode == Mode.Duplex)
            {
                try
                {
                    var data = Encoding.UTF8.GetString(buffer);

                    foreach (Match match in Regex.Matches(data, @"<event.+?\/event>"))
                    {
                        try
                        {
                            var evt = Event.Parse(match.Value);
                            Log.Information($"peer={_name} event=cot uid={evt.Uid} type={evt.Type}");
                            if (evt.IsA(CotPredicates.t_ping))
                            {
                                SendAsync(Event.Pong(evt).ToXmlString());
                                return;
                            }

                            _router.Send(evt, buffer);
                        }
                        catch (OverflowException)
                        {
                            Log.Error($"peer={_name} type=unknown error=true forwarded=false message=\"Overflow error. Receiving too much data.\"");
                        }
                        catch (Exception e)
                        {
                            Log.Error($"peer={_name} type=unknown error=true forwarded=false message=\"{e.Message}\"");
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"peer={_name} type=unknown error=true forwarded=false message=\"{e.Message}\"");
                }
            }
        }
    }
}
