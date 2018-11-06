using System.Collections.Generic;
using System.Threading;

namespace Rudel
{
    internal class ConcurrentQueueImpl<T>
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly Queue<T> _queue = new Queue<T>();

        public int Count
        {
            get
            {
                _lock.EnterReadLock();

                try
                {
                    return _queue.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public void Enqueue(T item)
        {
            _lock.EnterWriteLock();

            try
            {
                _queue.Enqueue(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }


        public bool TryDequeue(out T val)
        {
            _lock.EnterUpgradeableReadLock();

            try
            {
                val = default(T);

                if (_queue.Count > 0)
                {
                    _lock.EnterWriteLock();

                    try
                    {
                        val = _queue.Dequeue();
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                    return true;
                }

                return false;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public bool TryPeek(out T val)
        {
            _lock.EnterReadLock();

            try
            {
                val = default(T);

                if (_queue.Count > 0)
                {
                    val = _queue.Peek();

                    return true;
                }

                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();

            try
            {
                _queue.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
