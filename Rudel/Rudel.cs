using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Rudel
{
    public class Rudel
    {
        private bool _isRunning = false;
        private Thread _worker;
        private readonly SortedList<DateTime, ScheduledPacket> _queuedEvents = new SortedList<DateTime, ScheduledPacket>();
        private readonly ReaderWriterLockSlim _queuedEventsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public void Init()
        {
            _isRunning = true;
            _worker = new Thread(Worker);
            _worker.Start();
        }

        public void Shutdown()
        {
            _isRunning = false;
            _worker.Join();
        }

        private void Worker()
        {
            while (_isRunning)
            {
                // TODO: Remove this LINQ garbage
                if (_queuedEvents.Count > 0)
                {
                    ScheduledPacket packet = _queuedEvent.First();
                }
            }
        }
    }
}
