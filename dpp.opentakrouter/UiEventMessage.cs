using System;
using System.Text.Json;
using dpp.cot;

namespace dpp.opentakrouter
{
    public class UiEventMessage
    {
        public string Uid { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime Stale { get; set; }
        public DateTime Time { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Callsign { get; set; } = "";
        public string SourceId { get; set; } = "";

        public static UiEventMessage FromEnvelope(CotMessageEnvelope envelope)
        {
            return FromEvent(envelope?.Event, envelope?.SourceId);
        }

        public static UiEventMessage FromEvent(Event evt, string sourceId = "")
        {
            if (evt == null)
            {
                return null;
            }

            return new UiEventMessage
            {
                Uid = evt.Uid ?? "",
                Type = evt.Type ?? "",
                Stale = evt.Stale,
                Time = evt.Time,
                Lat = evt.Point?.Lat ?? 0.0,
                Lon = evt.Point?.Lon ?? 0.0,
                Callsign = evt.Detail?.Contact?.Callsign ?? evt.Uid ?? "",
                SourceId = sourceId ?? "",
            };
        }

        public static string Serialize(CotMessageEnvelope envelope)
        {
            return JsonSerializer.Serialize(FromEnvelope(envelope));
        }

        public static string Serialize(Event evt)
        {
            return JsonSerializer.Serialize(FromEvent(evt));
        }
    }
}
