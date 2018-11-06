using Rudel.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Rudel
{
    public class LocalPeer
    {
        internal readonly ConcurrentQueueImpl<NetworkEvent> incomingEvents = new ConcurrentQueueImpl<NetworkEvent>();

        public readonly Dictionary<EndPoint, RemotePeer> Connected = new Dictionary<EndPoint, RemotePeer>();

        internal readonly Dictionary<ConnectKey, RemotePeer> PendingPeers = new Dictionary<ConnectKey, RemotePeer>();

        public EndPoint LocalEndpoint { get; internal set; }
        private Socket socket;


        public void StartListening(EndPoint localEndpoint)
        {
            LocalEndpoint = localEndpoint;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(LocalEndpoint);
            ReceiveRecursive();
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

        public void Send(RemotePeer peer, byte channel, byte[] payload, int offset, int length)
        {
            ChanneledPacket packet = peer.Channels[channel].CreateOutgoingMessage(peer.SessionId, payload, offset, length);

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

            socket.BeginSendTo(payload, 0, size, SocketFlags.None, peer.RemoteEndpoint, (result) =>
            {
                int sentSize = socket.EndSendTo(result);
                peer.LastOutgoingMessageDate = DateTime.Now;
            }, socket);
        }

        internal void ReceiveRecursive()
        {
            byte[] payload = new byte[4096];

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint fromEndpoint = (EndPoint)sender;

            socket.BeginReceiveFrom(payload, 0, payload.Length, SocketFlags.None, ref fromEndpoint, (result) =>
            {
                int receivedSize = socket.EndReceiveFrom(result, ref fromEndpoint);
                HandlePacket(payload, receivedSize, fromEndpoint);

                ReceiveRecursive();

            }, socket);
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
                        if (connectionRequest.Read(data, length) && PendingPeers.ContainsKey(new ConnectKey(fromEndpoint, connectionRequest.ClientRandom)))
                        {
                            ulong localSessionPart = RandomUtils.GetULong(Constants.USE_CRYPTO_RANDOM);

                            RemotePeer peer = new RemotePeer()
                            {
                                RemoteSessionPart = connectionRequest.ClientRandom,
                                RemoteEndpoint = fromEndpoint,
                                ConnectionState = ConnectState.WaitingForChallengeResponse,
                                LocalSessionPart = localSessionPart,
                                SessionId = localSessionPart ^ connectionRequest.ClientRandom,
                                LastIncomingMessageDate = DateTime.Now
                            };

                            PendingPeers.Add(new ConnectKey(fromEndpoint, connectionRequest.ClientRandom), peer);

                            ChallengePacket packet = new ChallengePacket(peer.RemoteSessionPart, peer.LocalSessionPart);

                            for (int i = 0; i < Constants.CONNECTION_SEGMENT_RETRIES && peer.ConnectionState == ConnectState.WaitingForChallengeResponse; i++)
                            {
                                SendPacket(packet, peer);

                                Thread.Sleep(Constants.CONNECTION_SEGMENT_RETRY_TIMEOUT);
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
                                peer.LastIncomingMessageDate = DateTime.Now;
                                peer.RemoteSessionPart = challenge.ServerRandom;
                                peer.SessionId = peer.RemoteSessionPart ^ peer.LocalSessionPart;
                                peer.ConnectionState = ConnectState.Connected;

                                PendingPeers.Remove(new ConnectKey(fromEndpoint, challenge.ClientRandom));
                                Connected.Add(fromEndpoint, peer);

                                ChallengeResponsePacket packet = new ChallengeResponsePacket(peer.LocalSessionPart, peer.RemoteSessionPart, peer.SessionId);

                                for (int i = 0; i < Constants.CONNECTION_SEGMENT_RETRIES; i++)
                                {
                                    SendPacket(packet, peer);

                                    Thread.Sleep(Constants.CONNECTION_SEGMENT_RETRY_TIMEOUT);
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
                            if (peer.ConnectionState == ConnectState.WaitingForChallengeResponse && peer.SessionId == challengeResponse.ChallengeResponse)
                            {
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
