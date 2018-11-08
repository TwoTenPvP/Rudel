using NUnit.Framework;
using Rudel.Channels;
using Rudel.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Rudel.Utils;

namespace Rudel.Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestReliableSequencedChannelReceiveInSequence()
        {
            ReliableSequencedChannel sender = new ReliableSequencedChannel(0);
            ReliableSequencedChannel receiver = new ReliableSequencedChannel(0);

            Random random = new Random(0);

            for (int i = 0; i < 100000; i++)
            {
                RemotePeer fakePeer = new RemotePeer();// new IPEndPoint(IPAddress.Any, 5670));
                ulong sessionId = RandomUtils.GetULong(false);
                fakePeer.ChallengeResult = sessionId;
                int payLength = random.Next(0, 1024);
                byte[] payload = new byte[payLength];
                random.NextBytes(payload);
                ChanneledPacket sent = sender.CreateOutgoingMessage(payload, 0, payload.Length);
                byte[] buffer = new byte[5000];
                int oSize = sent.Write(buffer, fakePeer);
                ReliableSequencedPacket incPacket = (ReliableSequencedPacket)receiver.HandleIncomingMessagePoll(buffer, oSize, out bool hasMore);
                Assert.IsNotNull(incPacket, "it:" + i);
                Assert.AreEqual(payLength, incPacket.Payload.Length);
                CollectionAssert.AreEqual(payload, incPacket.Payload);
            }
        }

        [Test]
        public void TestReliableSequencedChannelReceiveOutOfSequence()
        {
            ReliableSequencedChannel sender = new ReliableSequencedChannel(0);
            ReliableSequencedChannel receiver = new ReliableSequencedChannel(0);

            RemotePeer firstPeer;
            ChanneledPacket firstPacket;
            {
                // Create first packet with sequence 1
                firstPeer = new RemotePeer();// (new IPEndPoint(IPAddress.Any, 5670));
                ulong sessionId = (ulong)0;
                firstPeer.ChallengeResult = sessionId;
                byte[] payload = new byte[0];
                firstPacket = sender.CreateOutgoingMessage(payload, 0, payload.Length);
            }
            {
                Random random = new Random(0);
                foreach (int i in Enumerable.Range(1, 100).OrderBy(x => random.Next()))
                {
                    RemotePeer fakePeer = new RemotePeer(); // (new IPEndPoint(IPAddress.Any, 5670));
                    ulong sessionId = (ulong)i;
                    fakePeer.ChallengeResult = sessionId;
                    byte[] payload = new byte[random.Next(0, 1024)];
                    random.NextBytes(payload);
                    ChanneledPacket sent = sender.CreateOutgoingMessage(payload, 0, payload.Length);
                    byte[] buffer = new byte[5000];
                    int oSize = sent.Write(buffer, fakePeer);
                    ReliableSequencedPacket incPacket = (ReliableSequencedPacket)receiver.HandleIncomingMessagePoll(buffer, oSize, out bool hasMore);
                    Assert.IsNull(incPacket);
                }
            }

            {
                byte[] buffer = new byte[5000];
                int oSize = firstPacket.Write(buffer, firstPeer);
                ReliableSequencedPacket incPacket = (ReliableSequencedPacket)receiver.HandleIncomingMessagePoll(buffer, oSize, out bool hasMore);

                Assert.NotNull(incPacket);
                Assert.IsTrue(incPacket.Payload.Length == 0);
                Assert.IsTrue(hasMore);
            }

            {
                Random random = new Random(0);
                foreach (int i in Enumerable.Range(1, 100).OrderBy(x => random.Next()))
                {
                    ulong sessionId = (ulong)i;
                    byte[] payload = new byte[random.Next(0, 1024)];
                    random.NextBytes(payload);
                    ReliableSequencedPacket incPacket = (ReliableSequencedPacket)receiver.HandlePoll();
                    Assert.NotNull(incPacket);
                    CollectionAssert.AreEqual(incPacket.Payload, payload);
                }
            }
        }

        [Test]
        public void TestSessionKey()
        {
            IPEndPoint endpoint1 = new IPEndPoint(IPAddress.Any, 5067);
            IPEndPoint endpoint2 = new IPEndPoint(IPAddress.Any, 5067);
            IPEndPoint endpoint3 = new IPEndPoint(IPAddress.Any, 5067);
            //Assert.True(endpoint1.Equals(endpoint2));

            Dictionary<ConnectKey, bool> keys = new Dictionary<ConnectKey, bool>
            {
                { new ConnectKey(endpoint1, 30), true }
            };
            Assert.True(keys.ContainsKey(new ConnectKey(endpoint2, 30)));
            Assert.True(keys[new ConnectKey(endpoint3, 30)]);
        }

        
        [Test]
        public void TestConnection()
        {
            RudelNetwork network = new RudelNetwork();
            network.Start();

            LocalPeer server = new LocalPeer(network);
            server.StartListening(new IPEndPoint(IPAddress.Any, 5057));



            LocalPeer client = new LocalPeer(network);
            client.StartListening(new IPEndPoint(IPAddress.Any, 5058));


            RemotePeer remotePeer = client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5057));

            while(true)
            {
                Debug.Print(remotePeer.ConnectionState.ToString());
            }
        }
        

        [Test]
        public void TestConnectionRequestPacket()
        {
            Random rnd = new Random(0);
            ulong val = (ulong)rnd.Next(int.MaxValue);
            ConnectionRequestPacket packet1 = new ConnectionRequestPacket(val);
            byte[] output = new byte[1024];
            int written = packet1.Write(output, null);

            ConnectionRequestPacket packet2 = new ConnectionRequestPacket();
            packet2.Read(output, written);

            Assert.AreEqual(packet1.ClientRandom, packet2.ClientRandom);

        }

        [Test]
        public void TestChallengePacket()
        {
            Random rnd = new Random(0);
            ulong clientRandom = (ulong)rnd.Next(int.MaxValue);
            ulong serverRandom = (ulong)rnd.Next(int.MaxValue);
            ChallengePacket packet1 = new ChallengePacket(clientRandom, serverRandom);
            byte[] output = new byte[1024];
            int written = packet1.Write(output, null);

            ChallengePacket packet2 = new ChallengePacket();
            packet2.Read(output, written);

            Assert.AreEqual(packet1.ClientRandom, packet2.ClientRandom);
            Assert.AreEqual(packet1.ServerRandom, packet2.ServerRandom);
        }

        [Test]
        public void TestChallengeResponsePacket()
        {
            Random rnd = new Random(0);
            ulong clientRandom = (ulong)rnd.Next(int.MaxValue);
            ulong serverRandom = (ulong)rnd.Next(int.MaxValue);
            ulong challengeResponse = (ulong)rnd.Next(int.MaxValue);
            ChallengeResponsePacket packet1 = new ChallengeResponsePacket(clientRandom, serverRandom, challengeResponse);
            byte[] output = new byte[1024];
            int written = packet1.Write(output, null);

            ChallengeResponsePacket packet2 = new ChallengeResponsePacket();
            packet2.Read(output, written);

            Assert.AreEqual(packet1.ClientRandom, packet2.ClientRandom);
            Assert.AreEqual(packet1.ServerRandom, packet2.ServerRandom);
            Assert.AreEqual(packet1.ChallengeResponse, packet2.ChallengeResponse);
        }
    }
}
