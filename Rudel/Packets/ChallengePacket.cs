namespace Rudel.Packets
{
    internal class ChallengePacket : Packet
    {
        public override PacketType PacketType => PacketType.Challenge;
        internal ulong ServerRandom { get; private set; }
        internal ulong ClientRandom { get; private set; }

        internal ChallengePacket(ulong clientRandom, ulong serverRandom)
        {
            this.ClientRandom = clientRandom;
            this.ServerRandom = serverRandom;
        }

        internal ChallengePacket()
        {

        }

        protected override int WritePacketBody(byte[] buffer, int offset)
        {
            // ClientRandom 
            for (int i = 0; i < sizeof(ulong); i++)
            {
                buffer[offset + i] = ((byte)(ClientRandom >> (i * 8)));
            }

            // ServerRandom
            for (int i = 0; i < sizeof(ulong); i++)
            {
                buffer[offset + sizeof(ulong) + i] = ((byte)(ServerRandom >> (i * 8)));
            }

            return (2 * sizeof(ulong));
        }

        protected override bool ReadPacketBody(byte[] buffer, int offset, int size)
        {
            if (size != (2 * sizeof(ulong))) return false;

            ClientRandom = buffer.ULongFromBytes(offset);

            ServerRandom = buffer.ULongFromBytes(offset + sizeof(ulong));

            return true;
        }
    }
}
