using System;

namespace Rudel.Packets
{
    public class ChanneledPacket : Packet
    {
        public override PacketType PacketType { get; } = PacketType.Invalid;
        public byte Channel { get; set; }

        public virtual bool HasSequence => false;
        public virtual ushort Sequence
        {
            get
            {
                if (HasSequence) throw new NotImplementedException();
                else throw new NotSupportedException();
            }
            internal set
            {
                if (HasSequence) throw new NotImplementedException();
                else throw new NotSupportedException();
            }
        }

        public virtual bool HasExplicitDeliveryStatus => false;
        public virtual ExplicitResponseState ExplicitResponse
        {
            get
            {
                if (HasExplicitDeliveryStatus) throw new NotImplementedException();
                else throw new NotSupportedException();
            }
            internal set
            {
                if (HasExplicitDeliveryStatus) throw new NotImplementedException();
                else throw new NotSupportedException();
            }
        }

        public virtual byte[] Payload => null;
        public virtual int Offset => 0;
        public virtual int Length => 0;

        internal ChanneledPacket(byte channel, PacketType packetType) : base(packetType)
        {
            Channel = channel;
        }

        internal ChanneledPacket()
        {

        }

        protected sealed override bool ReadPacketBody(byte[] buffer, int offset, int size)
        {
            Channel = buffer[offset];
            return ReadChannelMessageBody(buffer, offset + 1, size - 1);
        }

        protected sealed override int WritePacketBody(byte[] buffer, int offset)
        {
            buffer[offset] = Channel;
            return WriteChannelMessageBody(buffer, offset + 1) + 1;
        }

        protected virtual bool ReadChannelMessageBody(byte[] buffer, int offset, int size)
        {
            return false;
        }

        protected virtual int WriteChannelMessageBody(byte[] buffer, int offset)
        {
            return 0;
        }
    }
}
