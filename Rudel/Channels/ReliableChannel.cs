using Rudel.Packets;
using System.Collections.Generic;

namespace Rudel.Channels
{
    public class ReliableChannel : Channel
    {
        private ushort _lastOutboundSequenceNumber;

        private readonly SortedList<ushort, ReliablePacket> _incomingAckedPackets = new SortedList<ushort, ReliablePacket>();
        private ushort _incomingLowestAckedSequence;

        private readonly SortedList<ushort, ReliablePacket> _outgoingPendingMessages = new SortedList<ushort, ReliablePacket>();

        internal override bool SupportsAck => true;

        public ReliableChannel(byte channel) : base(channel)
        {
        }

        public override ChanneledPacket HandlePoll()
        {
            return null;
        }

        public override ChanneledPacket HandleIncomingMessagePoll(byte[] buffer, int size, out bool hasMore)
        {
            // Reliable has one message in equal no more than one out.
            hasMore = false;

            ReliablePacket packet = new ReliablePacket();
            packet.Read(buffer, size);

            if (packet.Sequence <= _incomingLowestAckedSequence || _incomingAckedPackets.ContainsKey(packet.Sequence))
            {
                // They didnt get our ack.

                // TODO: Send ack
                return null;
            }
            else if (packet.Sequence == _incomingLowestAckedSequence + 1)
            {
                // This is the "next" packet

                do
                {
                    // Remove previous
                    _incomingAckedPackets.Remove(_incomingLowestAckedSequence);
                    _incomingLowestAckedSequence++;
                }
                while (packet.Sequence == _incomingLowestAckedSequence + 1);

                // TODO: Send ack

                return packet;
            }
            else if (Distance(packet.Sequence, _incomingLowestAckedSequence) > 0 && !_incomingAckedPackets.ContainsKey(packet.Sequence))
            {
                // This is a future packet

                _incomingAckedPackets.Add(packet.Sequence, packet);

                // TODO: Send ack

                return packet;
            }

            return null;
        }

        private long Distance(ulong from, ulong to)
        {
            int _shift = (sizeof(ulong) - sizeof(ushort)) * sizeof(byte);

            to <<= _shift;
            from <<= _shift;

            return ((long)(from - to)) >> _shift;
        }

        public override ChanneledPacket CreateOutgoingMessage(byte[] payload, int offset, int length)
        {
            ReliablePacket message = new ReliablePacket(++_lastOutboundSequenceNumber, payload, offset, length);

            _outgoingPendingMessages.Add(message.Sequence, message);

            return message;
        }

        internal override void HandleAck(AckPacket packet)
        {
            if (_outgoingPendingMessages.ContainsKey(packet.Sequence))
            {
                _outgoingPendingMessages[packet.Sequence].ExplicitResponse = ExplicitResponseState.Ack;
                OnMessageAck(packet.Sequence);
                _outgoingPendingMessages.Remove(packet.Sequence);
            }
        }
    }
}
