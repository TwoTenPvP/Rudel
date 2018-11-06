using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rudel
{
    public class ScheduledPacket
    {
        public Packet Packet { get; set; }
        public LocalPeer LocalPeer { get; set; }
    }
}
