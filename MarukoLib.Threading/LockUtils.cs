using System;
using System.Threading;
using MarukoLib.Lang;

namespace MarukoLib.Threading
{
    public static class LockUtils
    {

        public static IDisposable AcquireReadLock(this ReaderWriterLockSlim @lock)
        {
            @lock.EnterReadLock();
            return new DelegatedDisposable(@lock.ExitReadLock);
        }

        public static IDisposable AcquireUpgradeableReadLock(this ReaderWriterLockSlim @lock)
        {
            @lock.EnterUpgradeableReadLock();
            return new DelegatedDisposable(@lock.ExitUpgradeableReadLock);
        }

        public static IDisposable AcquireWriteLock(this ReaderWriterLockSlim @lock)
        {
            @lock.EnterWriteLock();
            return new DelegatedDisposable(@lock.ExitWriteLock);
        }

    }
}
