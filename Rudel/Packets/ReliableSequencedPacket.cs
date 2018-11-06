using System;

namespace Rudel.Packets
{
    public sealed class ReliableSequencedPacket : ChanneledPacket
    {
        public override bool HasSequence => true;
        public override ushort Sequence { get; internal set; }
        public override bool HasExplicitDeliveryStatus => true;
        public override ExplicitResponseState ExplicitResponse { get; internal set; }

        internal ushort AckSequence { get; private set; }
        internal ulong AckMask { get; private set; }

        public override byte[] Payload => payload;
        public override int Offset => Offset;
        public override int Length => Length;

        private byte[] payload;
        private int offset;
        private int length;

        internal ReliableSequencedPacket(byte channel, ulong sessionId, ushort sequence, ushort ackSequence, ulong xackMask, byte[] payload, int offset, int length) : base(channel, PacketType.Data)
        {
            this.ExplicitResponse = ExplicitResponseState.None;
            this.Sequence = sequence;
            this.AckSequence = ackSequence;
            this.AckMask = xackMask;
            this.payload = payload;
            this.offset = offset;
            this.length = length;
        }

        internal ReliableSequencedPacket()
        {

        }

        protected sealed override bool ReadChannelMessageBody(byte[] buffer, int offset, int size)
        {
            // Can't be valid. Too short to fit all headers
            if (size < 12) return false;


            Sequence = buffer.UShortFromBytes(offset);

            AckSequence = buffer.UShortFromBytes(offset + sizeof(ushort));

            AckMask = buffer.ULongFromBytes(offset + (2 * sizeof(ushort)));

            // TODO: Remove
            payload = new byte[size - 12];
            Buffer.BlockCopy(buffer, offset + 12, Payload, 0, size - 12);
            return true;
        }

        protected sealed override int WriteChannelMessageBody(byte[] buffer, int offset)
        {
            for (int i = 0; i < sizeof(ushort); i++) buffer[offset + i] = ((byte)(Sequence >> (i * 8)));
            for (int i = 0; i < sizeof(ushort); i++) buffer[offset + 2 + i] = ((byte)(AckSequence >> (i * 8)));
            for (int i = 0; i < sizeof(ulong); i++) buffer[offset + 4 + i] = ((byte)(AckMask >> (i * 8)));

            Buffer.BlockCopy(Payload, 0, buffer, offset + 12, Payload.Length);

            return 12 + Payload.Length;
        }
    }
}
