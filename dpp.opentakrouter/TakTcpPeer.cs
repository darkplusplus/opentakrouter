using dpp.cot;
using NetCoreServer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpClient = NetCoreServer.TcpClient;

namespace dpp.opentakrouter
{
    public class TakTcpClient : TcpClient
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
        private string _name;

        public TakTcpClient(string name, string address, int port, IRouter router, Mode mode=Mode.Duplex, int minBackoff=3000, int maxBackoff=300000) : base(address, port)
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
                var msg = new cot.Message() { Event = e.Event };
                _ = Send(msg.ToXmlString());
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
        }

        protected override void OnDisconnected()
        {
            Log.Information($"peer={_name} state=reconnecting backoff={_backoff}");
            Thread.Sleep(_backoff);
            _backoff = Math.Clamp((int)Math.Round(_backoff * Math.E), _initialBackoff, _maxBackoff);

            if (!_stop)
                ConnectAsync();
        }

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"peer={_name} error={error}");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (_clientMode == Mode.Receive || _clientMode == Mode.Duplex)
            {
                try
                {
                    var msgstr = Encoding.UTF8.GetString(buffer);
                    var msg = Message.Parse(buffer, (int)offset, (int)size);
                    if (msg.Event.IsA(CotPredicates.t_ping))
                        return;

                    Log.Information($"peer={_name} event=cot type={msg.Event.Type}");
                    _router.Send(msg.Event);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"peer={_name} type=unknown error=true forwarded=false");
                }
            }
        }
    }
}
