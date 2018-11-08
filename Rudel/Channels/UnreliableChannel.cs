using Rudel.Packets;

namespace Rudel.Channels
{
    public class UnreliableChannel : Channel
    {
        private ushort _lastOutboundSequenceNumber;
        private readonly MessageSequencer<UnreliablePacket> _receiveSequencer = new MessageSequencer<UnreliablePacket>(Constants.DUPLICATION_PROTECTION_BUFFER_SIZE);

        public UnreliableChannel(byte channel) : base(channel)
        {
        }

        public override ChanneledPacket HandlePoll()
        {
            return null;
        }

        public override ChanneledPacket HandleIncomingMessagePoll(byte[] buffer, int size, out bool hasMore)
        {
            hasMore = false;

            UnreliablePacket packet = new UnreliablePacket();
            packet.Read(buffer, size);

            if (!_receiveSequencer.HasMessage(packet.Sequence))
            {
                _receiveSequencer.Push(packet);
                return packet;
            }

            return null;
        }

        public override ChanneledPacket CreateOutgoingMessage(byte[] payload, int offset, int length)
        {
            UnreliablePacket message = new UnreliablePacket(++_lastOutboundSequenceNumber, payload, offset, length);

            return message;
        }
    }
}