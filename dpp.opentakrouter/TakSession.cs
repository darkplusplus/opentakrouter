using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using dpp.cot;
using NetCoreServer;
using Serilog;

namespace dpp.opentakrouter
{
    public class TakSession : TcpSession
    {
        public TakSession(TcpServer server) : base(server)
        {
        }
        protected override void OnConnected()
        {
            Log.Information($"id={Id} endpoint={Socket.RemoteEndPoint.ToString()} state=connected");
        }

        protected override void OnDisconnected()
        {
            Log.Information($"id={Id} state=disconnected");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string msg = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            try
            {
                var evt = Event.Parse(msg);
                if (evt.Type == "t-x-c-t")
                {
                    Log.Information($"id={Id} endpoint={Socket.RemoteEndPoint.ToString()} type=event-cot-ping");
                    SendAsync(Event.Pong().ToXmlString());
                }
                else
                {
                    if (Server.Multicast(buffer, offset, size))
                    {
                        Log.Information($"id={Id} endpoint={Socket.RemoteEndPoint.ToString()} type=event-cot propogate=success data={msg}");
                    }
                    else
                    {
                        Log.Warning($"id={Id} endpoint={Socket.RemoteEndPoint.ToString()} type=event-cot propogate=failure data={msg}");
                    }
                }
            }
            catch (Exception e)
            {
                // TODO: figure out how to guard against propogating bullshit, but forward just in case
                _ = Server.Multicast(buffer, offset, size);
                Log.Error(e, "id={Id} endpoint={Socket.RemoteEndPoint.ToString()} type=event-cot error=true forwarded=true");
            }
            
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"id={Id} error={error}");
        }
    }
}
