using System;
using System.Collections.Generic;
using System.Linq;

namespace dpp.opentakrouter
{
    public enum RouteDirection
    {
        Inbound = 0,
        Outbound = 1,
    }

    public enum RouteAction
    {
        Allow = 0,
        Deny = 1,
    }

    public class RouteRule
    {
        public string Name { get; set; } = "";
        public RouteAction Action { get; set; } = RouteAction.Allow;
        public string[] SourcePrefixes { get; set; } = Array.Empty<string>();
        public string[] DestinationPrefixes { get; set; } = Array.Empty<string>();
        public string[] TypePrefixes { get; set; } = Array.Empty<string>();
        public double? MinLat { get; set; }
        public double? MaxLat { get; set; }
        public double? MinLon { get; set; }
        public double? MaxLon { get; set; }
        public bool? Persist { get; set; }
    }

    public class RoutePolicyConfig
    {
        public List<RouteRule> Inbound { get; set; } = new();
        public List<RouteRule> Outbound { get; set; } = new();
    }

    internal sealed class RouteDecision
    {
        public bool Allowed { get; set; } = true;
        public bool? Persist { get; set; }
        public string RuleName { get; set; } = "";
    }

    internal sealed class RoutePolicyEngine
    {
        private readonly IReadOnlyList<RouteRule> _inboundRules;
        private readonly IReadOnlyList<RouteRule> _outboundRules;

        public RoutePolicyEngine(RoutePolicyConfig config)
        {
            _inboundRules = config?.Inbound ?? new List<RouteRule>();
            _outboundRules = config?.Outbound ?? new List<RouteRule>();
        }

        public RouteDecision EvaluateInbound(CotMessageEnvelope envelope)
        {
            return Evaluate(_inboundRules, envelope, null);
        }

        public RouteDecision EvaluateOutbound(CotMessageEnvelope envelope, string destinationId)
        {
            return Evaluate(_outboundRules, envelope, destinationId);
        }

        private static RouteDecision Evaluate(IReadOnlyList<RouteRule> rules, CotMessageEnvelope envelope, string destinationId)
        {
            var decision = new RouteDecision();
            if ((rules == null) || (envelope?.Event == null))
            {
                return decision;
            }

            foreach (var rule in rules)
            {
                if (!Matches(rule, envelope, destinationId))
                {
                    continue;
                }

                decision.Allowed = rule.Action != RouteAction.Deny;
                decision.RuleName = rule.Name ?? "";
                if (rule.Persist.HasValue)
                {
                    decision.Persist = rule.Persist.Value;
                }
            }

            return decision;
        }

        private static bool Matches(RouteRule rule, CotMessageEnvelope envelope, string destinationId)
        {
            if (rule == null)
            {
                return false;
            }

            if (!MatchesPrefixes(rule.SourcePrefixes, envelope.SourceId))
            {
                return false;
            }

            if (!MatchesPrefixes(rule.DestinationPrefixes, destinationId))
            {
                return false;
            }

            if (!MatchesPrefixes(rule.TypePrefixes, envelope.Event?.Type))
            {
                return false;
            }

            return MatchesPoint(rule, envelope);
        }

        private static bool MatchesPrefixes(IEnumerable<string> prefixes, string value)
        {
            var candidates = prefixes?.Where(prefix => !string.IsNullOrWhiteSpace(prefix)).ToArray() ?? Array.Empty<string>();
            if (candidates.Length == 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return candidates.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesPoint(RouteRule rule, CotMessageEnvelope envelope)
        {
            var point = envelope.Event?.Point;
            if (point == null)
            {
                return !(rule.MinLat.HasValue || rule.MaxLat.HasValue || rule.MinLon.HasValue || rule.MaxLon.HasValue);
            }

            if (rule.MinLat.HasValue && point.Lat < rule.MinLat.Value)
            {
                return false;
            }

            if (rule.MaxLat.HasValue && point.Lat > rule.MaxLat.Value)
            {
                return false;
            }

            if (rule.MinLon.HasValue && point.Lon < rule.MinLon.Value)
            {
                return false;
            }

            if (rule.MaxLon.HasValue && point.Lon > rule.MaxLon.Value)
            {
                return false;
            }

            return true;
        }
    }
}
