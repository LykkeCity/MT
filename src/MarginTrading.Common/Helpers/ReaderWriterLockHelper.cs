using System;
using System.Threading;

namespace MarginTrading.Common.Helpers
{
    public class ReaderWriterLockHelper : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public void Dispose()
        {
            _lock?.Dispose();
        }

        public Disposable EnterReadLock()
        {
            _lock.EnterReadLock();
            return new Disposable(_lock, l => l.ExitReadLock());
        }

        public Disposable EnterWriteLock()
        {
            _lock.EnterWriteLock();
            return new Disposable(_lock, l => l.ExitWriteLock());
        }

        public Disposable EnterUpgradeableReadLock()
        {
            _lock.EnterUpgradeableReadLock();
            return new Disposable(_lock, l => l.ExitUpgradeableReadLock());
        }

        public struct Disposable : IDisposable
        {
            private readonly Action<ReaderWriterLockSlim> _onDispose;
            private readonly ReaderWriterLockSlim _locker;

            public Disposable(ReaderWriterLockSlim locker, Action<ReaderWriterLockSlim> onDispose)
            {
                _onDispose = onDispose;
                _locker = locker;
            }

            public void Dispose()
            {
                _onDispose(_locker);
            }
        }
    }
}