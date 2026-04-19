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
        private readonly RoutePolicyEngine _policyEngine;

        public Router(IConfiguration configuration, IClientRepository clients, IMessageRepository messages)
        {
            _configuration = configuration;
            _clients = clients;
            _messages = messages;

            _persistMessages = _configuration.GetValue("server:persist_messages", true);
            _policyEngine = new RoutePolicyEngine(_configuration.GetSection("server:routing").Get<RoutePolicyConfig>());
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

        public void Route(CotMessageEnvelope envelope)
        {
            var evt = envelope?.Event;
            if (evt == null)
            {
                return;
            }

            var inboundDecision = _policyEngine.EvaluateInbound(envelope);
            if (!inboundDecision.Allowed)
            {
                return;
            }

            if (evt.IsA(CotPredicates.t_ping))
            {
                _clients.Upsert(new Client()
                {
                    Callsign = evt.Detail?.Contact?.Callsign ?? "Unknown",
                    LastSeen = evt.Time,
                    Device = evt.Detail?.Takv?.Device ?? "Unknown",
                    Platform = evt.Detail?.Takv?.Platform ?? "Unknown",
                    Version = evt.Detail?.Takv?.Version ?? "Unknown"
                });

                return;
            }

            var shouldPersist = inboundDecision.Persist ?? _persistMessages;
            if (shouldPersist)
            {
                _messages.Upsert(new Models.StoredMessage()
                {
                    Uid = evt.Uid,
                    Data = evt.ToXmlString(),
                    Timestamp = evt.Time,
                    Expiration = evt.Stale
                });
            }

            OnRaiseRoutedEvent(new RoutedEventArgs(envelope));
        }

        protected virtual void OnRaiseRoutedEvent(RoutedEventArgs e)
        {
            RaiseRoutedEvent?.Invoke(this, e);
        }

        internal RouteDecision EvaluateOutbound(CotMessageEnvelope envelope, string destinationId)
        {
            return _policyEngine.EvaluateOutbound(envelope, destinationId);
        }

        public bool ShouldRouteTo(CotMessageEnvelope envelope, string destinationId)
        {
            return EvaluateOutbound(envelope, destinationId).Allowed;
        }
    }

    public class RoutedEventArgs : EventArgs
    {
        public CotMessageEnvelope Envelope { get; }
        public Event Event => Envelope.Event;

        public RoutedEventArgs(CotMessageEnvelope envelope)
        {
            Envelope = envelope;
        }
    }
}
