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
            server.StartListening(new IPEndPoint(IPAddress.Any, 1234));
            LocalPeer client = clientNetwork.CreateLocalPeer();
            client.StartListening(new IPEndPoint(IPAddress.Any, 1224));

            // Don't actually need this. You will get it in the connect event
            RemotePeer clientServerPeer = client.Connect(new IPEndPoint(IPAddress.Any, 1234));
            
            
            // TODO: Poll for messages
        }
    }
}