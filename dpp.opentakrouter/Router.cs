using dpp.cot;
using dpp.opentakrouter.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dpp.opentakrouter
{
    public class Router : IRouter
    {
        private readonly IClientRepository _clients;
        private readonly IMessageRepository _messages;

        public Router(IClientRepository clients, IMessageRepository messages)
        {
            _clients = clients;
            _messages = messages;
        }

        public event EventHandler<RoutedEventArgs> RaiseRoutedEvent;

        public void Send(Event e)                     
        {
            if (e.IsA(CotPredicates.t_ping))
            {
                _clients.Upsert(new Client()
                {
                    Callsign = e.Detail.Contact?.Callsign ?? "Unknown",
                    LastSeen = e.Time,
                    Device = e.Detail.Takv?.Device ?? "Unknown",
                    Platform = e.Detail.Takv?.Platform ?? "Unknown",
                    Version = e.Detail.Takv?.Version ?? "Unknown"
                });

                return;
            }

            _messages.Add(new Models.StoredMessage()
            {
                Uid = e.Uid,
                Data = e.ToXmlString(),
                Timestamp = e.Time,
                Expiration = e.Stale
            });
            
            OnRaiseRoutedEvent(new RoutedEventArgs(e));
        }

        protected virtual void OnRaiseRoutedEvent(RoutedEventArgs e)
        {
            RaiseRoutedEvent?.Invoke(this, e);
        }
    }

    public class RoutedEventArgs : EventArgs
    {
        public Event Event { get; set; }
        public RoutedEventArgs(Event e)
        {
            this.Event = e;
        }
    }
}
