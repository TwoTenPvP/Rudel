using System;
using System.Collections.Generic;
using System.Threading;

namespace Rudel
{
    public class PacketScheduler
    {
        private readonly SortedList<DateTime, ScheduledPacket> _queuedEvents = new SortedList<DateTime, ScheduledPacket>();
        private readonly ReaderWriterLockSlim _queuedEventsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public void Add(DateTime key, ScheduledPacket value)
        {
            _queuedEventsLock.EnterWriteLock();

            try
            {
                _queuedEvents.Add(key, value);
            }
            finally
            {
                _queuedEventsLock.ExitWriteLock();
            }
        }

        public bool TryGetPacket(out ScheduledPacket packet)
        {
            _queuedEventsLock.EnterUpgradeableReadLock();

            try
            {
                packet = null;

                if (_queuedEvents.Count > 0 && DateTime.Now >= _queuedEvents.Keys[0])
                {
                    packet = _queuedEvents.Values[0];

                    _queuedEventsLock.EnterWriteLock();

                    try
                    {
                        _queuedEvents.RemoveAt(0);
                    }
                    finally
                    {
                        _queuedEventsLock.ExitWriteLock();
                    }

                    return true;
                }
                
                return false;
            }
            finally
            {
                _queuedEventsLock.ExitUpgradeableReadLock();
            }
        }
    }
}