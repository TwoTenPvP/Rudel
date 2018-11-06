namespace Rudel.Packets
{
    internal class HailPacket : Packet
    {
        public override PacketType PacketType => PacketType.Hail;

        protected override int WritePacketBody(byte[] buffer, int offset)
        {
            for (int i = 0; i < Constants.HAIL_MESSAGE.Length; i++)
            {
                buffer[offset + i] = (byte)Constants.HAIL_MESSAGE[i];
            }

            return Constants.HAIL_MESSAGE.Length;
        }

        protected override bool ReadPacketBody(byte[] buffer, int offset, int size)
        {
            if (size != Constants.HAIL_MESSAGE.Length) return false;

            for (int i = 0; i < size; i++)
            {
                if ((byte)Constants.HAIL_MESSAGE[i] != buffer[offset + i]) return false;
            }

            return true;
        }
    }
}
