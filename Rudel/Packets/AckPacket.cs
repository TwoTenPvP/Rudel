namespace Rudel.Packets
{
    internal class AckPacket : ChanneledPacket
    {
        public override PacketType PacketType => PacketType.Ack;
        public override bool HasSequence => true;
        public override ushort Sequence { get; internal set; }

        protected override int WriteChannelMessageBody(byte[] buffer, int offset)
        {
            for (int i = 0; i < sizeof(ushort); i++)
            {
                buffer[offset + i] = (byte)(Sequence >> (i * 8));
            }
            return sizeof(ushort);
        }

        protected override bool ReadChannelMessageBody(byte[] buffer, int offset, int size)
        {
            if (size != sizeof(ushort)) return false;

            Sequence = buffer.UShortFromBytes(offset);

            return true;
        }
    }
}
