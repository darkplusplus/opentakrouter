namespace dpp.opentakrouter
{
    public enum TakProtocolPreference
    {
        XmlOnly = 0,
        PreferProtobuf = 1,
    }

    public enum TakWireFormat
    {
        StreamingXml = 0,
        StreamingProtobuf = 1,
    }

    public enum TakConnectionRole
    {
        Server = 0,
        Client = 1,
    }

    internal enum TakNegotiationState
    {
        Idle = 0,
        AwaitingResponse = 1,
        Complete = 2,
    }
}
