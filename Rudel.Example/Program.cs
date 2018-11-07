using System;
using System.Net;
using Rudel;

namespace Rude.Example
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            RudelNetwork serverNetwork = new RudelNetwork();
            byte serverReliableChannel = serverNetwork.AddChannel(ChannelType.ReliableSequenced);
            
            RudelNetwork clientNetwork = new RudelNetwork();
            byte clientReliableChannel = clientNetwork.AddChannel(ChannelType.ReliableSequenced);
            
            
            serverNetwork.Start();
            clientNetwork.Start();

            LocalPeer server = serverNetwork.CreateLocalPeer();
            server.StartListening(new IPEndPoint(IPAddress.Any, 4343));
            LocalPeer client = clientNetwork.CreateLocalPeer();
            client.StartListening(new IPEndPoint(IPAddress.Any, 3434));

            // Don't actually need this. You will get it in the connect event
            RemotePeer clientServerPeer = client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4343));


            // TODO: Poll for messages
            while (true)
            {
                NetworkEvent serverEvent = server.Poll();
                if (serverEvent != null)
                {
                    Console.WriteLine("@Server: " + serverEvent.EventType);
                }

                NetworkEvent clientEvent = client.Poll();
                if (clientEvent != null)
                {
                    Console.WriteLine("@Client: " + clientEvent.EventType);
                }
            }
        }
    }
}