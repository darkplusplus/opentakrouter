using System.Text;
using dpp.cot;

namespace dpp.opentakrouter
{
    internal static class TakMessageStreamParser
    {
        private static readonly byte[] EventStart = Encoding.ASCII.GetBytes("<event");
        private static readonly byte[] EventEnd = Encoding.ASCII.GetBytes("</event>");
        private static readonly byte[] XmlDeclaration = Encoding.ASCII.GetBytes("<?xml");

        public static bool TryParseXml(byte[] buffer, int offset, int length, out Message message, out int bytesConsumed)
        {
            message = null;
            bytesConsumed = 0;

            if ((buffer == null) || (length <= 0))
            {
                return false;
            }

            var xmlStart = IndexOf(buffer, offset, length, XmlDeclaration);
            var eventStart = IndexOf(buffer, offset, length, EventStart);

            if (eventStart < 0)
            {
                return false;
            }

            var eventEnd = IndexOf(buffer, eventStart, (offset + length) - eventStart, EventEnd);
            if (eventEnd < 0)
            {
                return false;
            }

            var eventLength = (eventEnd + EventEnd.Length) - eventStart;
            message = Message.Parse(buffer, eventStart, eventLength);
            bytesConsumed = (eventEnd + EventEnd.Length) - offset;

            if ((xmlStart >= 0) && (xmlStart < eventStart))
            {
                bytesConsumed = (eventEnd + EventEnd.Length) - offset;
            }

            return true;
        }

        public static bool TryParseProtobuf(byte[] buffer, int offset, int length, byte protocolVersion, out Message message, out int bytesConsumed)
        {
            return Message.TryParseStreaming(buffer, offset, length, protocolVersion, out message, out bytesConsumed);
        }

        private static int IndexOf(byte[] buffer, int offset, int length, byte[] pattern)
        {
            var end = offset + length - pattern.Length;
            for (var index = offset; index <= end; index++)
            {
                var matched = true;
                for (var patternIndex = 0; patternIndex < pattern.Length; patternIndex++)
                {
                    if (buffer[index + patternIndex] != pattern[patternIndex])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
