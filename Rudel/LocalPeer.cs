using Rudel.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Rudel
{
    public class LocalPeer
    {
        internal readonly ConcurrentQueueImpl<NetworkEvent> incomingEvents = new ConcurrentQueueImpl<NetworkEvent>();

        public readonly Dictionary<EndPoint, RemotePeer> Connected = new Dictionary<EndPoint, RemotePeer>();

        internal readonly Dictionary<ConnectKey, RemotePeer> PendingPeers = new Dictionary<ConnectKey, RemotePeer>();

        public EndPoint LocalEndpoint { get; internal set; }
        private Socket socket;
        private readonly RudelNetwork _network;

        internal LocalPeer(RudelNetwork network)
        {
            _network = network;
        }
        

        public void StartListening(EndPoint localEndpoint)
        {
            LocalEndpoint = localEndpoint;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(LocalEndpoint);
        }
        
        public RemotePeer Connect(EndPoint remoteEndpoint)
        {
            RemotePeer remotePeer = new RemotePeer()
            {
                ConnectionState = ConnectState.WaitingForChallenge,
                ChallengeSeed = RandomUtils.GetULong(Constants.USE_CRYPTO_RANDOM),
                RemoteEndpoint = remoteEndpoint
            };
            
            PendingPeers.Add(new ConnectKey(remoteEndpoint, remotePeer.ChallengeSeed), remotePeer);

            ConnectionRequestPacket packet = new ConnectionRequestPacket(remotePeer.ChallengeSeed);

            SendPacket(packet, remotePeer);
                            
            for (int i = 0; i < Constants.CONNECTION_SEGMENT_RETRIES - 1; i++)
            {
                _network.PacketScheduler.Add(DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, Constants.CONNECTION_SEGMENT_RETRY_TIMEOUT * i)), new ScheduledPacket()
                {
                    Packet = packet,
                    LocalPeer = this,
                    RemotePeer = remotePeer
                });
            }
            
            return remotePeer;
        }
        
        public NetworkEvent Poll()
        {
            if (incomingEvents.TryDequeue(out NetworkEvent @event))
            {
                return @event;
            }
            else
            {
                return null;
            }
        }

        public void Disconnect(EndPoint endpoint)
        {
            // TODO: Implement
        }

        public void Send(RemotePeer peer, byte channel, byte[] payload, int offset, int length)
        {
            ChanneledPacket packet = peer.Channels[channel].CreateOutgoingMessage(payload, offset, length);

            if (peer.ConnectionState != ConnectState.Connected)
            {
                // TODO: Shit hit the fan
            }

            SendPacket(packet, peer);
        }

        internal void SendPacket(Packet packet, RemotePeer peer)
        {
            byte[] payload = new byte[4096];
            int size = packet.Write(payload, peer);

            int sentSize = socket.SendTo(payload, 0, size, SocketFlags.None, peer.RemoteEndpoint);
            peer.LastOutgoingMessageDate = DateTime.Now;
        }

        internal void PollSocket()
        {
            if (socket.Poll(Constants.SOCKET_BLOCK_MILISECONDS * 1000, SelectMode.SelectError))
            {
                byte[] payload = new byte[4096];

                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint fromEndpoint = (EndPoint)sender;

                int size = socket.ReceiveFrom(payload, 0, payload.Length, SocketFlags.None, ref fromEndpoint);
                HandlePacket(payload, size, fromEndpoint);
            }
        }

        private void HandlePacket(byte[] data, int length, EndPoint fromEndpoint)
        {
            if (length < 1)
            {
                // TODO: Shit hit the fan
            }

            PacketType type = (PacketType)data[0];
            switch (type)
            {
                case PacketType.ConnectionRequest:
                    {
                        ConnectionRequestPacket connectionRequest = new ConnectionRequestPacket();
                        if (connectionRequest.Read(data, length) && !PendingPeers.ContainsKey(new ConnectKey(fromEndpoint, connectionRequest.ClientRandom)))
                        {
                            ulong localSessionPart = RandomUtils.GetULong(Constants.USE_CRYPTO_RANDOM);

                            RemotePeer peer = new RemotePeer()
                            {
                                ChallengeData = connectionRequest.ClientRandom,
                                RemoteEndpoint = fromEndpoint,
                                ConnectionState = ConnectState.WaitingForChallengeResponse,
                                ChallengeSeed = localSessionPart,
                                ChallengeResult = localSessionPart ^ connectionRequest.ClientRandom,
                                LastIncomingMessageDate = DateTime.Now
                            };

                            PendingPeers.Add(new ConnectKey(fromEndpoint, connectionRequest.ClientRandom), peer);

                            ChallengePacket packet = new ChallengePacket(peer.ChallengeData, peer.ChallengeSeed);

                            SendPacket(packet, peer);
                            
                            for (int i = 0; i < Constants.CONNECTION_SEGMENT_RETRIES - 1; i++)
                            {
                                _network.PacketScheduler.Add(DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, Constants.CONNECTION_SEGMENT_RETRY_TIMEOUT * i)), new ScheduledPacket()
                                {
                                    Packet = packet,
                                    LocalPeer = this,
                                    RemotePeer = peer
                                });
                            }
                        }
                    }
                    break;
                case PacketType.Challenge:
                    {
                        ChallengePacket challenge = new ChallengePacket();
                        if (challenge.Read(data, length) && PendingPeers.ContainsKey(new ConnectKey(fromEndpoint, challenge.ClientRandom)))
                        {
                            RemotePeer peer = PendingPeers[new ConnectKey(fromEndpoint, challenge.ClientRandom)];
                            if (peer.ConnectionState == ConnectState.WaitingForChallenge)
                            {
                                peer.CreateChannels(_network.ChannelTypes);
                                peer.LastIncomingMessageDate = DateTime.Now;
                                peer.ChallengeData = challenge.ServerRandom;
                                peer.ChallengeResult = peer.ChallengeData ^ peer.ChallengeSeed;
                                peer.ConnectionState = ConnectState.Connected;

                                PendingPeers.Remove(new ConnectKey(fromEndpoint, challenge.ClientRandom));
                                Connected.Add(fromEndpoint, peer);

                                ChallengeResponsePacket packet = new ChallengeResponsePacket(peer.ChallengeSeed, peer.ChallengeData, peer.ChallengeResult);

                                SendPacket(packet, peer);
                            
                                for (int i = 0; i < Constants.CONNECTION_SEGMENT_RETRIES - 1; i++)
                                {
                                    _network.PacketScheduler.Add(DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, Constants.CONNECTION_SEGMENT_RETRY_TIMEOUT * i)), new ScheduledPacket()
                                    {
                                        Packet = packet,
                                        LocalPeer = this,
                                        RemotePeer = peer
                                    });
                                }
                            }
                        }
                    }
                    break;
                case PacketType.ChallengeResponse:
                    {
                        ChallengeResponsePacket challengeResponse = new ChallengeResponsePacket();
                        if (challengeResponse.Read(data, length) && PendingPeers.ContainsKey(new ConnectKey(fromEndpoint, challengeResponse.ClientRandom)))
                        {
                            RemotePeer peer = PendingPeers[new ConnectKey(fromEndpoint, challengeResponse.ClientRandom)];
                            if (peer.ConnectionState == ConnectState.WaitingForChallengeResponse && peer.ChallengeResult == challengeResponse.ChallengeResponse)
                            {
                                peer.CreateChannels(_network.ChannelTypes);
                                peer.LastIncomingMessageDate = DateTime.Now;
                                peer.ConnectionState = ConnectState.Connected;
                                PendingPeers.Remove(new ConnectKey(fromEndpoint, challengeResponse.ClientRandom));
                                Connected.Add(fromEndpoint, peer);
                                incomingEvents.Enqueue(new NetworkEvent()
                                {
                                    EventType = EventType.Connect,
                                    Packet = null,
                                    RemotePeer = peer,
                                    LocalPeer = this
                                });
                            }
                        }
                    }
                    break;
                case PacketType.Data:
                    {
                        ChanneledPacket channeledPacket = new ChanneledPacket();
                        if (channeledPacket.Read(data, length) && Connected.ContainsKey(fromEndpoint))
                        {
                            RemotePeer peer = Connected[fromEndpoint];
                            if (peer.ConnectionState == ConnectState.Connected)
                            {
                                ChanneledPacket incomingPacket = peer.Channels[channeledPacket.Channel].HandleIncomingMessagePoll(data, length, out bool hasMore);
                                while (incomingPacket != null)
                                {
                                    peer.LastIncomingMessageDate = DateTime.Now;
                                    incomingEvents.Enqueue(new NetworkEvent()
                                    {
                                        EventType = EventType.Data,
                                        Packet = incomingPacket,
                                        RemotePeer = peer,
                                        LocalPeer = this
                                    });
                                    if (!hasMore) break;
                                    incomingPacket = peer.Channels[channeledPacket.Channel].HandlePoll();
                                }
                            }
                        }
                    }
                    break;
                case PacketType.Disconnect:
                    {
                        DisconnectPacket disconnectPacket = new DisconnectPacket();
                        if (disconnectPacket.Read(data, length) && Connected.ContainsKey(fromEndpoint))
                        {
                            RemotePeer peer = Connected[fromEndpoint];
                            peer.ConnectionState = ConnectState.Disconnected;
                            Connected.Remove(fromEndpoint);
                            incomingEvents.Enqueue(new NetworkEvent()
                            {
                                EventType = EventType.Disconnect,
                                Packet = null,
                                RemotePeer = peer,
                                LocalPeer = this
                            });
                        }
                    }
                    break;
                case PacketType.Ack:
                    {
                        AckPacket ackPacket = new AckPacket();
                        if (ackPacket.Read(data, length) && Connected.ContainsKey(fromEndpoint))
                        {
                            RemotePeer peer = Connected[fromEndpoint];
                            if (peer.Channels[ackPacket.Channel].SupportsAck)
                            {
                                peer.Channels[ackPacket.Channel].HandleAck(ackPacket);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
