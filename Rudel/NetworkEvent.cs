using Rudel.Packets;

namespace Rudel
{
    public class NetworkEvent
    {
        public EventType EventType;
        public ChanneledPacket Packet;
        public RemotePeer RemotePeer;
        public LocalPeer LocalPeer;
    }
}
