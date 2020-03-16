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
            return Disposables.For(@lock.ExitReadLock);
        }

        public static IDisposable AcquireUpgradeableReadLock(this ReaderWriterLockSlim @lock)
        {
            @lock.EnterUpgradeableReadLock();
            return Disposables.For(@lock.ExitUpgradeableReadLock);
        }

        public static IDisposable AcquireWriteLock(this ReaderWriterLockSlim @lock)
        {
            @lock.EnterWriteLock();
            return Disposables.For(@lock.ExitWriteLock);
        }

    }

}
