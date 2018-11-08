using Rudel.Packets;
using System;

namespace Rudel
{
    public abstract class Channel
    {
        public byte ChannelId { get; }
        public abstract ChanneledPacket HandlePoll();
        public abstract ChanneledPacket HandleIncomingMessagePoll(byte[] buffer, int length, out bool hasMore);
        public abstract ChanneledPacket CreateOutgoingMessage(byte[] payload, int offset, int length);

        internal virtual bool SupportsAck => false;
        
        internal Channel(byte channel)
        {
            ChannelId = channel;
        }

        internal virtual void HandleAck(AckPacket packet)
        {
            if (SupportsAck) throw new NotImplementedException("Not implemented");
            else throw new NotSupportedException("Ack is not supported on this channel");
        }

        protected void OnMessageAck(int sequence)
        {
            // Called when this channel gets an EXPLICITLY acked message
        }

        protected void OnMessageNack(int sequence)
        {
            // Called when this channel gets an EXPLICITLY nacked message
        }

        internal virtual void ResendPoll()
        {
            
        }
    }
}
