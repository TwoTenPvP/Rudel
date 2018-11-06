using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rudel
{
    public class RemotePeer
    {
        public ulong SessionId { get; internal set; }
        internal ulong RemoteSessionPart { get; set; }
        internal ulong LocalSessionPart { get; set; }
        public EndPoint RemoteEndpoint { get; internal set; }
        public ConnectState ConnectionState { get; internal set; } = ConnectState.Disconnected;
        public readonly Dictionary<byte, Channel> Channels = new Dictionary<byte, Channel>();
        public DateTime LastIncomingMessageDate = DateTime.MinValue;
        public DateTime LastOutgoingMessageDate = DateTime.MinValue;

        internal RemotePeer()
        {

        }
    }
}
