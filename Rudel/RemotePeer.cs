using System;
using System.Collections.Generic;
using System.Net;
using Rudel.Channels;

namespace Rudel
{
    public class RemotePeer
    {
        public ulong ChallengeResult { get; internal set; }
        internal ulong ChallengeData { get; set; }
        internal ulong ChallengeSeed { get; set; }
        public EndPoint RemoteEndpoint { get; internal set; }
        public ConnectState ConnectionState { get; internal set; } = ConnectState.Disconnected;
        public Channel[] Channels { get; private set; }
        public DateTime LastIncomingMessageDate = DateTime.MinValue;
        public DateTime LastOutgoingMessageDate = DateTime.MinValue;

        internal RemotePeer()
        {

        }

        internal void CreateChannels(List<ChannelType> types)
        {
            Channels = new Channel[types.Count];
            
            for (byte i = 0; i < types.Count; i++)
            {
                switch (types[i])
                {
                    case ChannelType.Reliable:
                    {
                        Channels[i] = new ReliableChannel(i);
                    }
                        break;
                    case ChannelType.ReliableSequenced:
                    {
                        Channels[i] = new ReliableSequencedChannel(i);
                    }
                        break;
                }
            }
        }
    }
}
