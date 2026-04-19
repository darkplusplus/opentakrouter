using dpp.cot;
using Xunit;

namespace dpp.opentakrouter.Tests
{
    public class RoutePolicyTests
    {
        [Fact]
        public void InboundRuleDeniesSourceAndTypeMatch()
        {
            var engine = new RoutePolicyEngine(new RoutePolicyConfig
            {
                Inbound =
                {
                    new RouteRule
                    {
                        Name = "deny-adsb",
                        Action = RouteAction.Deny,
                        SourcePrefixes = new[] { "peer:adsb" },
                        TypePrefixes = new[] { "a-" },
                    }
                }
            });

            var envelope = CotMessageEnvelope.FromEvent(
                new Event { Type = "a-f-A-M-F-Q", How = "m-g" },
                "peer:adsb-west",
                CotTransportKind.XmlStream);

            var decision = engine.EvaluateInbound(envelope);

            Assert.False(decision.Allowed);
            Assert.Equal("deny-adsb", decision.RuleName);
        }

        [Fact]
        public void InboundRuleMatchesBoundingBox()
        {
            var engine = new RoutePolicyEngine(new RoutePolicyConfig
            {
                Inbound =
                {
                    new RouteRule
                    {
                        Name = "bbox",
                        Action = RouteAction.Deny,
                        MinLat = 30,
                        MaxLat = 40,
                        MinLon = -120,
                        MaxLon = -110,
                    }
                }
            });

            var inside = CotMessageEnvelope.FromEvent(
                new Event
                {
                    Type = "a-f-G-U-C",
                    How = "m-g",
                    Point = new Point { Lat = 35, Lon = -115, Hae = 0, Ce = 1, Le = 1 }
                },
                "peer:test",
                CotTransportKind.XmlStream);

            var outside = CotMessageEnvelope.FromEvent(
                new Event
                {
                    Type = "a-f-G-U-C",
                    How = "m-g",
                    Point = new Point { Lat = 50, Lon = -115, Hae = 0, Ce = 1, Le = 1 }
                },
                "peer:test",
                CotTransportKind.XmlStream);

            Assert.False(engine.EvaluateInbound(inside).Allowed);
            Assert.True(engine.EvaluateInbound(outside).Allowed);
        }

        [Fact]
        public void InboundRuleCanOverridePersistence()
        {
            var engine = new RoutePolicyEngine(new RoutePolicyConfig
            {
                Inbound =
                {
                    new RouteRule
                    {
                        Name = "no-persist",
                        Action = RouteAction.Allow,
                        TypePrefixes = new[] { "b-" },
                        Persist = false,
                    }
                }
            });

            var envelope = CotMessageEnvelope.FromEvent(
                new Event { Type = "b-m-p-s-p-loc", How = "m-g" },
                "peer:test",
                CotTransportKind.XmlStream);

            var decision = engine.EvaluateInbound(envelope);

            Assert.True(decision.Allowed);
            Assert.False(decision.Persist);
        }

        [Fact]
        public void OutboundRuleDeniesSpecificDestination()
        {
            var engine = new RoutePolicyEngine(new RoutePolicyConfig
            {
                Outbound =
                {
                    new RouteRule
                    {
                        Name = "deny-ui",
                        Action = RouteAction.Deny,
                        DestinationPrefixes = new[] { "server:ws" },
                    }
                }
            });

            var envelope = CotMessageEnvelope.FromEvent(
                new Event { Type = "a-f-G-U-C", How = "m-g" },
                "peer:test",
                CotTransportKind.XmlStream);

            Assert.False(engine.EvaluateOutbound(envelope, "server:ws").Allowed);
            Assert.True(engine.EvaluateOutbound(envelope, "peer:other").Allowed);
        }

        [Fact]
        public void OutboundRuleMatchesSourceDestinationAndBoundingBox()
        {
            var engine = new RoutePolicyEngine(new RoutePolicyConfig
            {
                Outbound =
                {
                    new RouteRule
                    {
                        Name = "adsb-west-default-deny",
                        Action = RouteAction.Deny,
                        SourcePrefixes = new[] { "peer:adsb" },
                        DestinationPrefixes = new[] { "peer:regional-west" },
                    },
                    new RouteRule
                    {
                        Name = "adsb-west-box",
                        Action = RouteAction.Allow,
                        SourcePrefixes = new[] { "peer:adsb" },
                        DestinationPrefixes = new[] { "peer:regional-west" },
                        MinLat = 30,
                        MaxLat = 40,
                        MinLon = -120,
                        MaxLon = -110,
                    }
                }
            });

            var inside = CotMessageEnvelope.FromEvent(
                new Event
                {
                    Type = "a-f-A-M-F-Q",
                    How = "m-g",
                    Point = new Point { Lat = 35, Lon = -115, Hae = 0, Ce = 1, Le = 1 }
                },
                "peer:adsb-feed",
                CotTransportKind.XmlStream);

            var outside = CotMessageEnvelope.FromEvent(
                new Event
                {
                    Type = "a-f-A-M-F-Q",
                    How = "m-g",
                    Point = new Point { Lat = 45, Lon = -115, Hae = 0, Ce = 1, Le = 1 }
                },
                "peer:adsb-feed",
                CotTransportKind.XmlStream);

            Assert.True(engine.EvaluateOutbound(inside, "peer:regional-west").Allowed);
            Assert.False(engine.EvaluateOutbound(outside, "peer:regional-west").Allowed);
        }
    }
}
