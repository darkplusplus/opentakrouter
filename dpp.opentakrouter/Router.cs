using dpp.cot;
using dpp.opentakrouter.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace dpp.opentakrouter
{
    public class Router : IRouter
    {
        private readonly IConfiguration _configuration;
        private readonly IClientRepository _clients;
        private readonly IMessageRepository _messages;

        private readonly bool _persistMessages;

        public Router(IConfiguration configuration, IClientRepository clients, IMessageRepository messages)
        {
            _configuration = configuration;
            _clients = clients;
            _messages = messages;

            _persistMessages = _configuration.GetValue("server:persist_messages", false);
        }

        public event EventHandler<RoutedEventArgs> RaiseRoutedEvent;

        public IEnumerable<Event> GetActiveEvents()
        {
            List<Event> results = new();

            foreach (var evt in _messages.GetActive())
            {
                results.Add(Event.Parse(evt.Data));
            }

            return results;
        }

        public void Send(Event e, byte[] data)
        {
            data ??= (new Message() { Event = e }).ToXmlBytes();

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

            if (_persistMessages)
            {
                _messages.Upsert(new Models.StoredMessage()
                {
                    Uid = e.Uid,
                    Data = e.ToXmlString(),
                    Timestamp = e.Time,
                    Expiration = e.Stale
                });
            }

            OnRaiseRoutedEvent(new RoutedEventArgs(e, data));
        }

        protected virtual void OnRaiseRoutedEvent(RoutedEventArgs e)
        {
            RaiseRoutedEvent?.Invoke(this, e);
        }
    }

    public class RoutedEventArgs : EventArgs
    {
        public Event Event { get; set; }
        public byte[] Data { get; set; }
        public RoutedEventArgs(Event e, byte[] data)
        {
            Event = e;
            Data = data ?? (new Message() { Event = e }).ToXmlBytes();
        }
    }
}
