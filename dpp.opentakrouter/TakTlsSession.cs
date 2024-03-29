﻿using dpp.cot;
using NetCoreServer;
using Serilog;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace dpp.opentakrouter
{
    public class TakTlsSession : SslSession
    {
        private readonly IRouter _router;
        private const string _component = "tak-ssl";
        public TakTlsSession(TakTlsServer server) : base(server)
        {
            _router = server.Router;
        }
        protected override void OnConnected()
        {
            Log.Information($"server=tak-ssl endpoint={Socket.RemoteEndPoint} session={Id} state=connected");
            foreach (var evt in _router.GetActiveEvents())
            {
                Send(evt.ToXmlString());
            }
        }

        protected override void OnDisconnected()
        {
            Log.Information($"server=tak-ssl session={Id} state=disconnected");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                var data = Encoding.UTF8.GetString(buffer);

                foreach (Match match in Regex.Matches(data, @"<event.+?\/event>"))
                {
                    try
                    {
                        var evt = Event.Parse(match.Value);
                        Log.Information($"server={_component} endpoint={Socket.RemoteEndPoint} session={Id} event=cot uid={evt.Uid} type={evt.Type}");
                        if (evt.IsA(CotPredicates.t_ping))
                        {
                            SendAsync(Event.Pong(evt).ToXmlString());
                            return;
                        }

                        _router.Send(evt, buffer);
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
                Log.Error($"server={_component} endpoint={Socket.RemoteEndPoint} session={Id} type=unknown error=true forwarded=false message=\"{e.Message}\"");
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=tak-ssl endpoint={Socket.RemoteEndPoint} session={Id} error=true message=\"{error}\"");
        }
    }
}
