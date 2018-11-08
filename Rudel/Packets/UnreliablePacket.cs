using System;

namespace Rudel.Packets
{
    public class UnreliablePacket : ChanneledPacket
    {
        public override bool HasSequence => true;
        public override ushort Sequence { get; internal set; }
        public override bool HasExplicitDeliveryStatus => true;
        public override ExplicitResponseState ExplicitResponse { get; internal set; }

        public override byte[] Payload => payload;
        public override int Offset => Offset;
        public override int Length => Length;

        private byte[] payload;
        private int offset;
        private int length;

        internal UnreliablePacket(ushort sequence, byte[] payload, int offset, int length)
        {
            this.ExplicitResponse = ExplicitResponseState.None;
            this.Sequence = sequence;
            this.payload = payload;
            this.offset = offset;
            this.length = length;
        }

        internal UnreliablePacket()
        {

        }

        protected sealed override bool ReadChannelMessageBody(byte[] buffer, int offset, int size)
        {
            // TODO: Ensure header is enough size??
            Sequence = buffer.UShortFromBytes(offset);

            // TODO: Remove this, borrow some memory instead
            payload = new byte[size - 2];
            Buffer.BlockCopy(buffer, offset + 2, Payload, 0, Payload.Length);

            return true;
        }

        protected sealed override int WriteChannelMessageBody(byte[] buffer, int offset)
        {
            for (int i = 0; i < sizeof(ushort); i++) buffer[offset + i] = ((byte)(Sequence >> (i * 8)));

            Buffer.BlockCopy(Payload, 0, buffer, offset + 2, Payload.Length);

            return 2 + Payload.Length;
        }
    }
}