using Rudel.Packets;

namespace Rudel
{
    internal class MessageSequencer<T> where T : ChanneledPacket
    {
        private readonly VirtualOverflowArray<T> _pendingMessages;
        
        internal MessageSequencer(int size)
        {
            _pendingMessages = new VirtualOverflowArray<T>(size);
        }

        internal void Push(T message)
        {
            _pendingMessages[message.Sequence] = message;
        }

        internal T Peek(int index)
        {
            return _pendingMessages[index];
        }

        internal bool HasMessage(int index)
        {
            return _pendingMessages[index] != null;
        }
    }
}
