using System.Text;
using dpp.cot;
using Xunit;

namespace dpp.opentakrouter.Tests
{
    public class TakConnectionProtocolTests
    {
        private const string SupportAdvertisementXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<event version='2.0' uid='protouid' type='t-x-takp-v' time='2022-02-02T22:22:22Z' start='2022-02-02T22:22:22Z' stale='2022-02-02T22:32:22Z' how='m-g'>" +
            "<point lat='0.0' lon='0.0' hae='0.0' ce='999999' le='999999'/>" +
            "<detail><TakControl><TakProtocolSupport version='1'/></TakControl></detail>" +
            "</event>";

        private const string NegotiationAcceptedXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<event version='2.0' uid='protouid' type='t-x-takp-r' time='2022-02-02T22:22:22Z' start='2022-02-02T22:22:22Z' stale='2022-02-02T22:32:22Z' how='m-g'>" +
            "<point lat='0.0' lon='0.0' hae='0.0' ce='999999' le='999999'/>" +
            "<detail><TakControl><TakResponse status='true'/></TakControl></detail>" +
            "</event>";

        [Fact]
        public void XmlParserExtractsTraditionalCotMessage()
        {
            var bytes = Encoding.UTF8.GetBytes(SupportAdvertisementXml);

            Assert.True(TakMessageStreamParser.TryParseXml(bytes, 0, bytes.Length, out var message, out var consumed));
            Assert.Equal(bytes.Length, consumed);
            Assert.Equal("t-x-takp-v", message.Event.Type);
        }

        [Fact]
        public void ClientNegotiatesToProtobufWhenServerAdvertisesSupport()
        {
            var protocol = new TakConnectionProtocol(TakConnectionRole.Client, TakProtocolPreference.PreferProtobuf);
            var advertiseBytes = Encoding.UTF8.GetBytes(SupportAdvertisementXml);
            var acceptBytes = Encoding.UTF8.GetBytes(NegotiationAcceptedXml);

            var advertiseResult = Assert.Single(protocol.Read(advertiseBytes, 0, advertiseBytes.Length, "peer:test"));
            Assert.NotNull(advertiseResult.ControlResponse);
            Assert.Equal(TakWireFormat.StreamingXml, advertiseResult.NewWireFormat);

            var acceptResult = Assert.Single(protocol.Read(acceptBytes, 0, acceptBytes.Length, "peer:test"));
            Assert.True(acceptResult.WireFormatChanged);
            Assert.Equal(TakWireFormat.StreamingProtobuf, acceptResult.NewWireFormat);
        }

        [Fact]
        public void ServerAcceptsProtobufNegotiationRequest()
        {
            var protocol = new TakConnectionProtocol(TakConnectionRole.Server, TakProtocolPreference.PreferProtobuf);
            var request = new Message
            {
                Event = new Event
                {
                    Version = "2.0",
                    Uid = "protouid",
                    Type = "t-x-takp-q",
                    How = "m-g",
                    Detail = new Detail
                    {
                        AdditionalElements = new[]
                        {
                            ParseElement("<TakControl><TakRequest version='1'/></TakControl>")
                        }
                    }
                }
            };

            var requestBytes = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + request.ToXmlString());

            var result = Assert.Single(protocol.Read(requestBytes, 0, requestBytes.Length, "server:test"));
            Assert.NotNull(result.ControlResponse);
            Assert.True(result.WireFormatChanged);
            Assert.Equal(TakWireFormat.StreamingProtobuf, result.NewWireFormat);
        }

        [Fact]
        public void EnvelopeDefaultsMessageFromEvent()
        {
            var evt = new Event { Uid = "test", Type = "a-f-G-U-C", How = "m-g" };

            var envelope = CotMessageEnvelope.FromEvent(evt, "source", CotTransportKind.HttpApi);

            Assert.NotNull(envelope.Message);
            Assert.Same(evt, envelope.Event);
            Assert.Equal("source", envelope.SourceId);
        }

        private static System.Xml.XmlElement ParseElement(string xml)
        {
            var document = new System.Xml.XmlDocument();
            document.LoadXml(xml);
            return document.DocumentElement;
        }
    }
}
