namespace Rudel
{
    public abstract class Packet
    {
        public abstract PacketType PacketType { get; }
        private PacketType packetType = PacketType.Invalid;

        internal Packet()
        {

        }

        internal Packet(PacketType packetType)
        {
            this.packetType = packetType;
        }

        internal bool Read(byte[] buffer, int size)
        {
            this.packetType = (PacketType)buffer[0];

            return ReadPacketBody(buffer, 1, size - 1);
        }

        internal int Write(byte[] buffer, RemotePeer peer)
        {
            buffer[0] = (byte)PacketType;

            return WritePacketBody(buffer, 1) + 1;
        }

        protected abstract int WritePacketBody(byte[] buffer, int offset);
        protected abstract bool ReadPacketBody(byte[] buffer, int offset, int size);
    }
}
