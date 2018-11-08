using System;
using System.Collections.Generic;
using System.Threading;

namespace Rudel
{
    public class RudelNetwork
    {
        private bool _isRunning = false;
        private Thread _worker;
        private readonly List<LocalPeer> _localPeers = new List<LocalPeer>();
        internal readonly List<ChannelType> ChannelTypes = new List<ChannelType>();
        internal readonly PacketScheduler PacketScheduler = new PacketScheduler();
        
        public void Start()
        {
            _isRunning = true;
            _worker = new Thread(Worker);
            _worker.Start();
        }

        public void Shutdown()
        {
            _worker.Join();
            _isRunning = false;
        }

        public LocalPeer CreateLocalPeer()
        {
            LocalPeer peer = new LocalPeer(this);
            _localPeers.Add(peer);
            
            return peer;
        }

        public byte AddChannel(ChannelType type)
        {
            if (_isRunning) throw new Exception("Cannot add channel while running");
            ChannelTypes.Add(type);
            return (byte) (ChannelTypes.Count - 1);
        }
        
        private void Worker()
        {
            while (_isRunning)
            {
                CheckScheduledPackets();
                CheckTimeout();
                SendHeartbeats();
                PollSockets();
            }
        }

        private void CheckScheduledPackets()
        {
            while (PacketScheduler.TryGetPacket(out ScheduledPacket packet))
            {
                packet.LocalPeer.SendPacket(packet.Packet, packet.RemotePeer);
            }
        }

        private void CheckTimeout()
        {
            for (int i = 0; i < _localPeers.Count; i++)
            {
                foreach (RemotePeer peer in _localPeers[i].Connected.Values)
                {
                    if ((DateTime.Now - peer.LastIncomingMessageDate).TotalMilliseconds > Constants.CONNECTION_TIMEOUT_MILISECONDS)
                    {
                        // TODO: Timeout
                    }
                }
                
                foreach (RemotePeer peer in _localPeers[i].PendingPeers.Values)
                {
                    if ((DateTime.Now - peer.LastIncomingMessageDate).TotalMilliseconds > Constants.CONNECTION_TIMEOUT_MILISECONDS)
                    {
                        // TODO: Timeout
                    }
                }
            }
        }

        private void SendHeartbeats()
        {
            for (int i = 0; i < _localPeers.Count; i++)
            {
                foreach (RemotePeer peer in _localPeers[i].Connected.Values)
                {
                    if ((DateTime.Now - peer.LastOutgoingMessageDate).TotalMilliseconds > Constants.MAX_OUTGOING_MESSAGE_DELAY)
                    {
                        // TODO: Send heartbeat
                    }
                }
            }
        }

        private void PollSockets()
        {
            for (int i = 0; i < _localPeers.Count; i++)
            {
                _localPeers[i].PollSocket();
            }
        }

        private void PollChannels()
        {
            for (int i = 0; i < _localPeers.Count; i++)
            {
                foreach (RemotePeer peer in _localPeers[i].Connected.Values)
                {
                    for (int j = 0; j < peer.Channels.Length; j++)
                    {
                        peer.Channels[j].ResendPoll();
                    }
                }
            }
        }
    }
}
