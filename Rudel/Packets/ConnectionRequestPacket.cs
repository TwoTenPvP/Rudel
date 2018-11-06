namespace Rudel.Packets
{
    internal class ConnectionRequestPacket : Packet
    {
        public override PacketType PacketType => PacketType.ConnectionRequest;
        internal ulong ClientRandom { get; private set; }

        internal ConnectionRequestPacket(ulong clientRandom)
        {
            this.ClientRandom = clientRandom;
        }

        internal ConnectionRequestPacket()
        {

        }

        protected override int WritePacketBody(byte[] buffer, int offset)
        {
            // Write Protocol Prefix
            for (int i = 0; i < Constants.PROTOCOL_ESTABLISHMENT_PREFIX.Length; i++)
            {
                buffer[offset + i] = Constants.PROTOCOL_ESTABLISHMENT_PREFIX[i];
            }

            // ClientRandom 
            for (int i = 0; i < sizeof(ulong); i++)
            {
                buffer[offset + Constants.PROTOCOL_ESTABLISHMENT_PREFIX.Length + i] = ((byte)(ClientRandom >> (i * 8)));
            }

            return Constants.DOS_AMP_PROTECTION_SIZE - 1;
        }

        protected override bool ReadPacketBody(byte[] buffer, int offset, int size)
        {
            if (size != Constants.DOS_AMP_PROTECTION_SIZE - 1) return false;

            for (int i = 0; i < Constants.PROTOCOL_ESTABLISHMENT_PREFIX.Length; i++)
            {
                if (Constants.PROTOCOL_ESTABLISHMENT_PREFIX[i] != buffer[offset + i]) return false;
            }

            ClientRandom = buffer.ULongFromBytes(offset + Constants.PROTOCOL_ESTABLISHMENT_PREFIX.Length);

            return true;
        }
    }
}
