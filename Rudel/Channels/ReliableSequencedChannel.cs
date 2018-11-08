using System;
using Rudel.Packets;
using Rudel.Utils;

namespace Rudel.Channels
{
    internal sealed class ReliableSequencedChannel : Channel
    {
        private ushort _lowestAckedMessage;
        private ushort _lastOutboundSequenceNumber;
        private ushort _lastReceivedSequenceNumber;
        private readonly MessageSequencer<ReliableSequencedPacket> _receiveSequencer = new MessageSequencer<ReliableSequencedPacket>(Constants.SEQUENCE_MESSAGE_BUFFER_SIZE);
        private readonly MessageSequencer<ReliableSequencedPacket> _sendSequencer = new MessageSequencer<ReliableSequencedPacket>(Constants.SEQUENCE_MESSAGE_BUFFER_SIZE);

        internal override bool SupportsAck => true;

        public ReliableSequencedChannel(byte channel) : base(channel)
        {
        }
        
        public override ChanneledPacket CreateOutgoingMessage(byte[] payload, int offset, int length)
        {
            ReliableSequencedPacket message = new ReliableSequencedPacket(ChannelId, ++_lastOutboundSequenceNumber, _lastReceivedSequenceNumber, payload, offset, length);

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


            for (ushort i = packet.Sequence; _sendSequencer.HasMessage(i) && _sendSequencer.Peek(i).ExplicitResponse == ExplicitResponseState.Ack; i++)
            {
                _lowestAckedMessage = i;
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
            
            for (ushort i = packet.AckSequence; _sendSequencer.HasMessage(i) && _sendSequencer.Peek(i).ExplicitResponse == ExplicitResponseState.Ack; i++)
            {
                _lowestAckedMessage = i;
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

        internal override void ResendPoll()
        {
            long distance = SequencingUtils.Distance(_lastOutboundSequenceNumber, _lowestAckedMessage, sizeof(ushort));
            for (ushort i = _lowestAckedMessage; i < _lowestAckedMessage + distance; i++)
            {
                if (_sendSequencer.HasMessage(i) && (DateTime.Now - _sendSequencer.Peek(i).LastSent).TotalMilliseconds > Constants.RESEND_DELAY)
                {
                    _sendSequencer.Peek(i).LastSentBy.SendPacket(_sendSequencer.Peek(i), _sendSequencer.Peek(i).LastSentTo);
                }
            }
        }
    }
}
