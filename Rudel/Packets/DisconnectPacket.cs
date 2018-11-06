namespace Rudel.Packets
{
    internal class DisconnectPacket : Packet
    {
        public override PacketType PacketType => PacketType.Disconnect;

        protected override int WritePacketBody(byte[] buffer, int offset)
        {
            for (int i = 0; i < Constants.BYE_MESSAGE.Length; i++)
            {
                buffer[offset + i] = (byte)Constants.BYE_MESSAGE[i];
            }

            return Constants.BYE_MESSAGE.Length;
        }

        protected override bool ReadPacketBody(byte[] buffer, int offset, int size)
        {
            if (size != Constants.BYE_MESSAGE.Length) return false;

            for (int i = 0; i < size; i++)
            {
                if ((byte)Constants.BYE_MESSAGE[i] != buffer[offset + i]) return false;
            }

            return true;
        }
    }
}
