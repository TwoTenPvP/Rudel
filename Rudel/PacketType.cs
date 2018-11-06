namespace Rudel
{
    public enum PacketType
    {
        Invalid = 0,
        ConnectionRequest,
        Challenge,
        ChallengeResponse,
        Hail,
        Data,
        Ack,
        Disconnect
    }
}
