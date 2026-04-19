using System;

namespace dpp.opentakrouter
{
    internal static class TakProtocolPreferences
    {
        public static TakProtocolPreference Parse(string value)
        {
            return string.Equals(value, "prefer-protobuf", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "protobuf", StringComparison.OrdinalIgnoreCase)
                ? TakProtocolPreference.PreferProtobuf
                : TakProtocolPreference.XmlOnly;
        }
    }
}
