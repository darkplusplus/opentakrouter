using dpp.cot;

namespace dpp.opentakrouter
{
    public enum CotTransportKind
    {
        Unknown = 0,
        XmlStream = 1,
        TakProtobufStream = 2,
        WebSocketText = 3,
        HttpApi = 4,
    }

    public class CotMessageEnvelope
    {
        public Event Event { get; set; }
        public Message Message { get; set; }
        public string SourceId { get; set; } = "";
        public CotTransportKind TransportKind { get; set; } = CotTransportKind.Unknown;
        public byte[] RawData { get; set; }

        public static CotMessageEnvelope FromEvent(Event evt, string sourceId = "", CotTransportKind transportKind = CotTransportKind.Unknown, byte[] rawData = null)
        {
            return new CotMessageEnvelope
            {
                Event = evt,
                Message = new Message() { Event = evt },
                SourceId = sourceId ?? "",
                TransportKind = transportKind,
                RawData = rawData,
            };
        }
    }
}
