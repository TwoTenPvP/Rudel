using Rudel.Packets;

namespace Rudel.Channels
{
    internal sealed class ReliableSequencedChannel : Channel
    {
        private ushort _lastOutboundSequenceNumber;
        private ushort _lastReceivedSequenceNumber;
        private readonly MessageSequencer<ReliableSequencedPacket> _receiveSequencer = new MessageSequencer<ReliableSequencedPacket>();
        private readonly MessageSequencer<ReliableSequencedPacket> _sendSequencer = new MessageSequencer<ReliableSequencedPacket>();

        internal override bool SupportsAck => true;

        public ReliableSequencedChannel(byte channel) : base(channel)
        {
        }

        private ulong GetXAckMask(ushort lastReceived)
        {
            ulong mask = 0UL;
            for (byte i = 0; i < Constants.ACK_MASK_BITS; ++i)
            {
                if (_receiveSequencer.HasMessage(lastReceived + (Constants.ACK_MASK_BITS - 1 - i)))
                {
                    mask |= (1UL << i);
                }
            }

            return mask;
        }

        public override ChanneledPacket CreateOutgoingMessage(byte[] payload, int offset, int length)
        {
            ReliableSequencedPacket message = new ReliableSequencedPacket(ChannelId, ++_lastOutboundSequenceNumber, _lastReceivedSequenceNumber, GetXAckMask(_lastReceivedSequenceNumber), payload, offset, length);

            _sendSequencer.Push(message);

            return message;
        }

        internal override void HandleAck(AckPacket packet)
        {
            if (_sendSequencer.HasMessage(packet.Sequence) && _sendSequencer.Peek(packet.Sequence).ExplicitResponse != ExplicitResponseState.Ack)
            {
                _sendSequencer.Peek(packet.Sequence).ExplicitResponse = ExplicitResponseState.Ack;
                OnMessageAck(packet.Sequence);
            }
        }

        public override ChanneledPacket HandlePoll()
        {
            if (_receiveSequencer.HasMessage(_lastReceivedSequenceNumber + 1))
            {
                return _receiveSequencer.Peek(++_lastReceivedSequenceNumber);
            }

            return null;
        }

        public override ChanneledPacket HandleIncomingMessagePoll(byte[] buffer, int length, out bool hasMore)
        {
            ReliableSequencedPacket packet = new ReliableSequencedPacket();
            packet.Read(buffer, length);

            _receiveSequencer.Push(packet);

            // Set the ack
            if (_sendSequencer.HasMessage(packet.AckSequence) && _sendSequencer.Peek(packet.AckSequence).ExplicitResponse != ExplicitResponseState.Ack)
            {
                packet.ExplicitResponse = ExplicitResponseState.Ack;
                OnMessageAck(packet.AckSequence);
            }

            // Resolve their acks & nacks from mask
            for (int i = 0; i < Constants.ACK_MASK_BITS; i++)
            {
                if (_sendSequencer.HasMessage(packet.AckSequence - i))
                {
                    ExplicitResponseState newState = (packet.AckMask & (1UL << i)) != 0 ? ExplicitResponseState.Ack : ExplicitResponseState.Nack;
                    ExplicitResponseState currentState = _sendSequencer.Peek(packet.AckSequence - i).ExplicitResponse;

                    if (currentState != newState && currentState != ExplicitResponseState.Ack)
                    {
                        if (newState == ExplicitResponseState.Ack)
                            OnMessageAck(packet.AckSequence - i);
                        if (newState == ExplicitResponseState.Nack)
                            OnMessageNack(packet.AckSequence - i);
                    }

                    _sendSequencer.Peek(packet.AckSequence - i).ExplicitResponse = newState;
                }
            }

            if (_receiveSequencer.HasMessage((ushort)(_lastReceivedSequenceNumber + 1)))
            {
                hasMore = _receiveSequencer.HasMessage((ushort)(_lastReceivedSequenceNumber + 2));
                return _receiveSequencer.Peek(++_lastReceivedSequenceNumber);
            }
            else
            {
                hasMore = false;
                return null;
            }
        }
    }
}
