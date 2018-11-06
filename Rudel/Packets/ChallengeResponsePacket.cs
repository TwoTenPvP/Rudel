namespace Rudel.Packets
{
    internal class ChallengeResponsePacket : Packet
    {
        public override PacketType PacketType => PacketType.ChallengeResponse;
        internal ulong ClientRandom { get; private set; }
        internal ulong ServerRandom { get; private set; }
        internal ulong ChallengeResponse { get; private set; }

        internal ChallengeResponsePacket(ulong clientRandom, ulong serverRandom, ulong challengeResponse)
        {
            this.ClientRandom = clientRandom;
            this.ServerRandom = serverRandom;
            this.ChallengeResponse = challengeResponse;
        }

        internal ChallengeResponsePacket()
        {

        }

        protected override int WritePacketBody(byte[] buffer, int offset)
        {
            // Copy ClientRandom
            for (int i = 0; i < sizeof(ulong); i++)
            {
                buffer[offset + i] = (byte)(ClientRandom >> (i * 8));
            }

            // Copy ServerRandom
            for (int i = 0; i < sizeof(ulong); i++)
            {
                buffer[offset + sizeof(ulong) + i] = (byte)(ServerRandom >> (i * 8));
            }

            // Copy ChallengeResponse
            for (int i = 0; i < sizeof(ulong); i++)
            {
                buffer[offset + (2 * sizeof(ulong)) + i] = (byte)(ChallengeResponse >> (i * 8));
            }

            return Constants.DOS_AMP_PROTECTION_SIZE - 1;
        }

        protected override bool ReadPacketBody(byte[] buffer, int offset, int size)
        {
            if (size != Constants.DOS_AMP_PROTECTION_SIZE - 1) return false;

            ClientRandom = buffer.ULongFromBytes(offset);

            ServerRandom = buffer.ULongFromBytes(offset + sizeof(ulong));

            ChallengeResponse = buffer.ULongFromBytes(offset + (2 * sizeof(ulong)));

            return true;
        }
    }
}
