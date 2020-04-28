using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MarukoLib.Lang
{

    public static class LockUtils
    {

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static ReaderWriterLockSlim RWLock() => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static ReaderWriterLockSlim RecursiveRWLock() => new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

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
