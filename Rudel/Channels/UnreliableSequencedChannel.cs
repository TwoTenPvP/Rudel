using Rudel.Packets;
using Rudel.Utils;

namespace Rudel.Channels
{
    public class UnreliableSequencedChannel : Channel
    {
        private ushort _lastOutboundSequenceNumber;
        private ushort _lastReceivedSequenceNumber;
        private readonly MessageSequencer<UnreliableSequencedPacket> _sendSequencer = new MessageSequencer<UnreliableSequencedPacket>(Constants.ACK_MASK_BITS);
        private readonly MessageSequencer<UnreliableSequencedPacket> _receiveSequencer = new MessageSequencer<UnreliableSequencedPacket>(Constants.ACK_MASK_BITS);
        
        internal override bool SupportsAck => true;

        public UnreliableSequencedChannel(byte channel) : base(channel)
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
            UnreliableSequencedPacket message = new UnreliableSequencedPacket(ChannelId, ++_lastOutboundSequenceNumber, _lastReceivedSequenceNumber, GetXAckMask(_lastReceivedSequenceNumber), payload, offset, length);

            _sendSequencer.Push(message);

            return message;
        }

        internal override void HandleAck(AckPacket packet)
        {
            
        }

        public override ChanneledPacket HandlePoll()
        {
            return null;
        }

        public override ChanneledPacket HandleIncomingMessagePoll(byte[] buffer, int length, out bool hasMore)
        {
            hasMore = false;
            
            UnreliableSequencedPacket packet = new UnreliableSequencedPacket();
            packet.Read(buffer, length);

            long distance = SequencingUtils.Distance(packet.Sequence, _lastReceivedSequenceNumber, sizeof(ushort));
            if (distance > 0)
            {
                // This packet is a future packet
                _lastReceivedSequenceNumber = packet.Sequence;
                _receiveSequencer.Push(packet);
            }

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


            return distance > 0 ? packet : null;
        }
    }
}