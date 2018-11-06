using System;
using System.Net;

namespace Rudel
{
    internal class ConnectKey : IEquatable<ConnectKey>
    {
        public EndPoint EndPoint { get; }
        public ulong SessionId { get; }

        public ConnectKey(EndPoint endpoint, ulong sessionId)
        {
            this.EndPoint = endpoint;
            this.SessionId = sessionId;
        }

        public bool Equals(ConnectKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(EndPoint, other.EndPoint) && SessionId == other.SessionId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ConnectKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((EndPoint != null ? EndPoint.GetHashCode() : 0) * 397) ^ SessionId.GetHashCode();
            }
        }
    }
}
