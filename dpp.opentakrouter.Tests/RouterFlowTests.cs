using System;
using System.Collections.Generic;
using System.Linq;
using dpp.cot;
using dpp.opentakrouter.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace dpp.opentakrouter.Tests
{
    public class RouterFlowTests
    {
        [Fact]
        public void RouterAppliesOutboundFiltersPerDestination()
        {
            var router = CreateRouter(new Dictionary<string, string>
            {
                ["server:routing:outbound:0:Name"] = "deny-west-default",
                ["server:routing:outbound:0:Action"] = "Deny",
                ["server:routing:outbound:0:SourcePrefixes:0"] = "peer:adsb",
                ["server:routing:outbound:0:DestinationPrefixes:0"] = "peer:regional-west",
                ["server:routing:outbound:1:Name"] = "allow-west-box",
                ["server:routing:outbound:1:Action"] = "Allow",
                ["server:routing:outbound:1:SourcePrefixes:0"] = "peer:adsb",
                ["server:routing:outbound:1:DestinationPrefixes:0"] = "peer:regional-west",
                ["server:routing:outbound:1:MinLat"] = "30",
                ["server:routing:outbound:1:MaxLat"] = "40",
                ["server:routing:outbound:1:MinLon"] = "-120",
                ["server:routing:outbound:1:MaxLon"] = "-110",
            });

            var inside = CotMessageEnvelope.FromEvent(
                new Event
                {
                    Uid = "air-1",
                    Type = "a-f-A-M-F-Q",
                    How = "m-g",
                    Point = new Point { Lat = 35, Lon = -115, Hae = 0, Ce = 1, Le = 1 },
                },
                "peer:adsb-feed",
                CotTransportKind.XmlStream);

            var outside = CotMessageEnvelope.FromEvent(
                new Event
                {
                    Uid = "air-2",
                    Type = "a-f-A-M-F-Q",
                    How = "m-g",
                    Point = new Point { Lat = 45, Lon = -115, Hae = 0, Ce = 1, Le = 1 },
                },
                "peer:adsb-feed",
                CotTransportKind.XmlStream);

            Assert.True(router.ShouldRouteTo(inside, "peer:regional-west"));
            Assert.False(router.ShouldRouteTo(outside, "peer:regional-west"));
            Assert.True(router.ShouldRouteTo(outside, "peer:regional-east"));
        }

        [Fact]
        public void RoutePolicyCanMatchCallsignUidAndFreshness()
        {
            var engine = new RoutePolicyEngine(new RoutePolicyConfig
            {
                Inbound =
                {
                    new RouteRule
                    {
                        Name = "deny-rest",
                        Action = RouteAction.Deny,
                    },
                    new RouteRule
                    {
                        Name = "allow-watch",
                        Action = RouteAction.Allow,
                        Uids = new[] { "watch-1" },
                        Callsigns = new[] { "EAGLE01" },
                        MaxAgeSeconds = 30,
                    },
                }
            });

            var fresh = CotMessageEnvelope.FromEvent(
                new Event
                {
                    Uid = "watch-1",
                    Type = "a-f-G-U-C",
                    How = "m-g",
                    Time = DateTime.UtcNow.AddSeconds(-10),
                    Detail = new Detail { Contact = new Contact { Callsign = "EAGLE01" } }
                },
                "peer:test",
                CotTransportKind.XmlStream);

            var stale = CotMessageEnvelope.FromEvent(
                new Event
                {
                    Uid = "watch-1",
                    Type = "a-f-G-U-C",
                    How = "m-g",
                    Time = DateTime.UtcNow.AddMinutes(-10),
                    Detail = new Detail { Contact = new Contact { Callsign = "EAGLE01" } }
                },
                "peer:test",
                CotTransportKind.XmlStream);

            Assert.True(engine.EvaluateInbound(fresh).Allowed);
            Assert.False(engine.EvaluateInbound(stale).Allowed);
        }

        [Fact]
        public void RouterPersistsUiPayloadsForActiveEventFeed()
        {
            var router = CreateRouter();
            router.Route(CotMessageEnvelope.FromEvent(
                new Event
                {
                    Uid = "u-1",
                    Type = "a-f-G-U-C",
                    How = "m-g",
                    Point = new Point { Lat = 1, Lon = 2, Hae = 0, Ce = 1, Le = 1 },
                    Detail = new Detail { Contact = new Contact { Callsign = "OTR01" } }
                },
                "api/events",
                CotTransportKind.HttpApi));

            var active = router.GetActiveEvents().Single();
            var payload = UiEventMessage.ToPayload(CotMessageEnvelope.FromEvent(active, "api/events", CotTransportKind.HttpApi));

            Assert.NotNull(payload);
        }

        private static Router CreateRouter(IDictionary<string, string> values = null)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values ?? new Dictionary<string, string>())
                .Build();
            return new Router(configuration, new FakeClientRepository(), new FakeMessageRepository());
        }

        private sealed class FakeClientRepository : IClientRepository
        {
            public int Add(Client c) => 1;
            public int Delete(string callsign) => 1;
            public Client Get(string callsign) => null;
            public IEnumerable<Client> Search(string keyword = "") => Enumerable.Empty<Client>();
            public int Update(Client c) => 1;
            public int Upsert(Client c) => 1;
        }

        private sealed class FakeMessageRepository : IMessageRepository
        {
            private readonly Dictionary<string, StoredMessage> _messages = new();

            public int Add(StoredMessage m)
            {
                _messages[m.Uid] = m;
                return 1;
            }

            public int Delete(string uid)
            {
                _messages.Remove(uid);
                return 1;
            }

            public int EvictExpired() => 0;
            public StoredMessage Get(string uid) => _messages.TryGetValue(uid, out var message) ? message : null;
            public IEnumerable<StoredMessage> GetActive() => _messages.Values;
            public IEnumerable<StoredMessage> Search(string keyword = "") => _messages.Values;
            public int Update(StoredMessage m)
            {
                _messages[m.Uid] = m;
                return 1;
            }

            public int Upsert(StoredMessage m)
            {
                _messages[m.Uid] = m;
                return 1;
            }
        }
    }
}
