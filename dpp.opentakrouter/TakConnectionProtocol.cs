using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using dpp.cot;

namespace dpp.opentakrouter
{
    internal sealed class TakConnectionProtocol
    {
        private const byte ProtobufProtocolVersion = 0x01;
        private static readonly byte[] XmlDeclaration = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        private readonly List<byte> _receiveBuffer = new();

        public TakConnectionProtocol(TakConnectionRole role, TakProtocolPreference preference)
        {
            Role = role;
            Preference = preference;
            Reset();
        }

        public TakConnectionRole Role { get; }

        public TakProtocolPreference Preference { get; }

        public TakWireFormat ActiveWireFormat { get; private set; }

        public void Reset()
        {
            _receiveBuffer.Clear();
            ActiveWireFormat = TakWireFormat.StreamingXml;
            NegotiationState = TakNegotiationState.Idle;
        }

        public IEnumerable<byte[]> GetInitialMessages()
        {
            if ((Role == TakConnectionRole.Server) && (Preference == TakProtocolPreference.PreferProtobuf))
            {
                yield return BuildStreamingXmlMessage(CreateProtocolAdvertisement());
            }
        }

        public byte[] Serialize(CotMessageEnvelope envelope)
        {
            if (envelope?.Event == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var message = envelope.Message ?? new Message() { Event = envelope.Event };
            return ActiveWireFormat switch
            {
                TakWireFormat.StreamingProtobuf => message.ToStreamingBytes(ProtobufProtocolVersion),
                _ => BuildStreamingXmlMessage(message),
            };
        }

        public IEnumerable<ProtocolReadResult> Read(byte[] buffer, int offset, int size, string sourceId)
        {
            for (var index = 0; index < size; index++)
            {
                _receiveBuffer.Add(buffer[offset + index]);
            }

            while (_receiveBuffer.Count > 0)
            {
                var snapshot = _receiveBuffer.ToArray();
                Message message;
                int bytesConsumed;

                var parsed = ActiveWireFormat == TakWireFormat.StreamingProtobuf
                    ? TakMessageStreamParser.TryParseProtobuf(snapshot, 0, snapshot.Length, ProtobufProtocolVersion, out message, out bytesConsumed)
                    : TakMessageStreamParser.TryParseXml(snapshot, 0, snapshot.Length, out message, out bytesConsumed);

                if (!parsed)
                {
                    yield break;
                }

                _receiveBuffer.RemoveRange(0, bytesConsumed);
                var previousWireFormat = ActiveWireFormat;
                var receivedWireFormat = previousWireFormat;
                var controlMessage = ProcessNegotiation(message);

                yield return new ProtocolReadResult
                {
                    Envelope = CotMessageEnvelope.FromEvent(
                        message.Event,
                        sourceId,
                        receivedWireFormat == TakWireFormat.StreamingProtobuf ? CotTransportKind.TakProtobufStream : CotTransportKind.XmlStream,
                        Slice(snapshot, bytesConsumed)),
                    ControlResponse = controlMessage is null ? null : BuildStreamingXmlMessage(controlMessage),
                    WireFormatChanged = previousWireFormat != ActiveWireFormat,
                    NewWireFormat = ActiveWireFormat,
                };
            }
        }

        private Message ProcessNegotiation(Message message)
        {
            if ((Preference != TakProtocolPreference.PreferProtobuf) || (message?.Event == null))
            {
                return null;
            }

            if (Role == TakConnectionRole.Client)
            {
                if ((ActiveWireFormat == TakWireFormat.StreamingXml) &&
                    (NegotiationState == TakNegotiationState.Idle) &&
                    SupportsProtocolVersion(message, ProtobufProtocolVersion))
                {
                    NegotiationState = TakNegotiationState.AwaitingResponse;
                    return CreateProtocolRequest(ProtobufProtocolVersion);
                }

                if ((ActiveWireFormat == TakWireFormat.StreamingXml) &&
                    (NegotiationState == TakNegotiationState.AwaitingResponse) &&
                    TryGetResponseStatus(message, out var accepted))
                {
                    NegotiationState = accepted ? TakNegotiationState.Complete : TakNegotiationState.Idle;
                    if (accepted)
                    {
                        ActiveWireFormat = TakWireFormat.StreamingProtobuf;
                    }
                }
            }
            else
            {
                if ((ActiveWireFormat == TakWireFormat.StreamingXml) &&
                    (message.Event.Type == "t-x-takp-q") &&
                    TryGetRequestedVersion(message, out var requestedVersion))
                {
                    var accepted = requestedVersion == ProtobufProtocolVersion;
                    if (accepted)
                    {
                        ActiveWireFormat = TakWireFormat.StreamingProtobuf;
                        NegotiationState = TakNegotiationState.Complete;
                    }

                    return CreateProtocolResponse(accepted);
                }
            }

            return null;
        }

        private TakNegotiationState NegotiationState { get; set; }

        private static byte[] BuildStreamingXmlMessage(Message message)
        {
            var xmlBytes = Encoding.UTF8.GetBytes(message.ToXmlString());
            var payload = new byte[XmlDeclaration.Length + xmlBytes.Length];
            System.Buffer.BlockCopy(XmlDeclaration, 0, payload, 0, XmlDeclaration.Length);
            System.Buffer.BlockCopy(xmlBytes, 0, payload, XmlDeclaration.Length, xmlBytes.Length);
            return payload;
        }

        private static Message CreateProtocolAdvertisement()
        {
            return CreateProtocolControlMessage("t-x-takp-v", "TakProtocolSupport", child =>
            {
                child.SetAttribute("version", ProtobufProtocolVersion.ToString());
            });
        }

        private static Message CreateProtocolRequest(byte version)
        {
            return CreateProtocolControlMessage("t-x-takp-q", "TakRequest", child =>
            {
                child.SetAttribute("version", version.ToString());
            });
        }

        private static Message CreateProtocolResponse(bool accepted)
        {
            return CreateProtocolControlMessage("t-x-takp-r", "TakResponse", child =>
            {
                child.SetAttribute("status", accepted ? "true" : "false");
            });
        }

        private static Message CreateProtocolControlMessage(string eventType, string controlChildName, Action<XmlElement> configure)
        {
            var document = new XmlDocument();
            var control = document.CreateElement("TakControl");
            var child = document.CreateElement(controlChildName);
            configure(child);
            control.AppendChild(child);

            return new Message
            {
                Event = new Event
                {
                    Version = "2.0",
                    Uid = "protouid",
                    Type = eventType,
                    How = "m-g",
                    Point = new Point
                    {
                        Lat = 0.0,
                        Lon = 0.0,
                        Hae = 0.0,
                        Ce = 999999.0,
                        Le = 999999.0,
                    },
                    Detail = new Detail
                    {
                        AdditionalElements = new[] { control },
                    },
                },
            };
        }

        private static bool SupportsProtocolVersion(Message message, byte version)
        {
            if ((message?.Event?.Type != "t-x-takp-v") || (message.Event.Detail?.AdditionalElements == null))
            {
                return false;
            }

            foreach (var control in message.Event.Detail.AdditionalElements)
            {
                if ((control == null) || !IsElement(control, "TakControl"))
                {
                    continue;
                }

                foreach (XmlNode child in control.ChildNodes)
                {
                    if ((child is XmlElement element) &&
                        IsElement(element, "TakProtocolSupport") &&
                        byte.TryParse(element.GetAttribute("version"), out var supportedVersion) &&
                        (supportedVersion == version))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetRequestedVersion(Message message, out byte version)
        {
            version = 0;
            if ((message?.Event?.Type != "t-x-takp-q") || (message.Event.Detail?.AdditionalElements == null))
            {
                return false;
            }

            foreach (var control in message.Event.Detail.AdditionalElements)
            {
                if ((control == null) || !IsElement(control, "TakControl"))
                {
                    continue;
                }

                foreach (XmlNode child in control.ChildNodes)
                {
                    if ((child is XmlElement element) &&
                        IsElement(element, "TakRequest") &&
                        byte.TryParse(element.GetAttribute("version"), out version))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetResponseStatus(Message message, out bool accepted)
        {
            accepted = false;
            if ((message?.Event?.Type != "t-x-takp-r") || (message.Event.Detail?.AdditionalElements == null))
            {
                return false;
            }

            foreach (var control in message.Event.Detail.AdditionalElements)
            {
                if ((control == null) || !IsElement(control, "TakControl"))
                {
                    continue;
                }

                foreach (XmlNode child in control.ChildNodes)
                {
                    if ((child is XmlElement element) &&
                        IsElement(element, "TakResponse") &&
                        bool.TryParse(element.GetAttribute("status"), out accepted))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsElement(XmlElement element, string name)
        {
            return string.Equals(element.LocalName, name, StringComparison.Ordinal) ||
                   string.Equals(element.Name, name, StringComparison.Ordinal);
        }

        private static byte[] Slice(byte[] data, int length)
        {
            var result = new byte[length];
            System.Buffer.BlockCopy(data, 0, result, 0, length);
            return result;
        }
    }

    internal sealed class ProtocolReadResult
    {
        public CotMessageEnvelope Envelope { get; set; }
        public byte[] ControlResponse { get; set; }
        public bool WireFormatChanged { get; set; }
        public TakWireFormat NewWireFormat { get; set; }
    }
}
