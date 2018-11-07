namespace Rudel
{
    public class ScheduledPacket
    {
        public Packet Packet { get; set; }
        public LocalPeer LocalPeer { get; set; }
        public RemotePeer RemotePeer { get; set; }
    }
}
