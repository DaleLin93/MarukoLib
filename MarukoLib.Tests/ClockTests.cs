using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MarukoLib.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class ClockTests
    {

        public const int ToleranceInMillis = 400;

        [TestMethod]
        public void TestSyncedClock()
        {
            var clock0 = Clocks.SystemTicksClock;
            var clock1 = Clocks.SystemTicksClock.Adjust(+100);
            Debug.WriteLine($"Test1: time0: {clock0.Time}, time1: {clock1.Time}");
            Assert.AreEqual(clock0.Time, clock1.Time - 100, TimeUnit.Millisecond.ConvertTo(1, clock0.Unit));

            /* Manually synced */
            var synced = SyncedClock.Sync(clock0, clock1.Time);
            Debug.WriteLine($"Test2: synced: {clock0.Time}, time1: {clock1.Time}");
            Assert.AreEqual(synced.Time, clock1.Time, TimeUnit.Millisecond.ConvertTo(1, clock0.Unit));

            /* Delay garenteed */
            var random = new Random();
            Assert.IsFalse(SyncedClock.TrySync(clock0, () =>
            {
                SpinWait.SpinUntil(() => false, random.Next(110, 150));
                return clock1.Time;
            }, TimeSpan.FromMilliseconds(5), 10, out _));
            Assert.IsTrue(SyncedClock.TrySync(clock0, () => clock1.Time, TimeSpan.FromMilliseconds(5), 15, out synced));
            Debug.WriteLine($"Test3: synced: {clock0.Time}, time1: {clock1.Time}");
            Assert.AreEqual(synced.Time, clock1.Time, TimeUnit.Millisecond.ConvertTo(6, clock0.Unit));
        }

        [TestMethod]
        public void TestFreezableClock()
        {
            var et = 0;
            var clock = new FreezableClock(AlignedClock.FromNow(Clocks.SystemTicksClock.As(TimeUnit.Millisecond)));
            
            Thread.Sleep(2000);
            et += 2000;
            var t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(Math.Abs(et - t) < ToleranceInMillis);

            Thread.Sleep(1000);
            et += 1000;
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(Math.Abs(et - t) < ToleranceInMillis);

            clock.Freeze();
            Thread.Sleep(2000);
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(Math.Abs(et - t) < ToleranceInMillis);

            clock.Unfreeze();
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(Math.Abs(et - t) < ToleranceInMillis);

            Thread.Sleep(2000);
            et += 2000;
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(Math.Abs(et - t) < ToleranceInMillis);
        }

        [TestMethod]
        public void TestFreezableClockAsync()
        {
            var et = 0;
            var clock = new FreezableClock(AlignedClock.FromNow(Clocks.SystemTicksClock.As(TimeUnit.Millisecond)));

            Task.Delay(3000).ContinueWith(task =>
            {
                clock.Freeze();
                Thread.Sleep(1000);
                clock.Unfreeze();
            });

            Thread.Sleep(2000);
            et += 2000;
            var t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(Math.Abs(et - t) < ToleranceInMillis);

            Thread.Sleep(5000);
            et += 4000;
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(Math.Abs(et - t) < ToleranceInMillis);
        }

    }

}
