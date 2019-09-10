using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using MarukoLib.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class ClockTests
    {

        public const int ToleranceInMillis = 400;

        [TestMethod]
        public void TestFreezableClock()
        {
            var et = 0;
            var clock = new FreezableClock(AlignedClock.FromNow(Clock.SystemTicksClock.As(TimeUnit.Millisecond)));
            
            Thread.Sleep(2000);
            et += 2000;
            var t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(System.Math.Abs(et - t) < ToleranceInMillis);

            Thread.Sleep(1000);
            et += 1000;
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(System.Math.Abs(et - t) < ToleranceInMillis);

            clock.Freeze();
            Thread.Sleep(2000);
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(System.Math.Abs(et - t) < ToleranceInMillis);

            clock.Unfreeze();
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(System.Math.Abs(et - t) < ToleranceInMillis);

            Thread.Sleep(2000);
            et += 2000;
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(System.Math.Abs(et - t) < ToleranceInMillis);
        }

        [TestMethod]
        public void TestFreezableClockAsync()
        {
            var et = 0;
            var clock = new FreezableClock(AlignedClock.FromNow(Clock.SystemTicksClock.As(TimeUnit.Millisecond)));

            System.Threading.Tasks.Task.Delay(3000).ContinueWith(task =>
            {
                clock.Freeze();
                Thread.Sleep(1000);
                clock.Unfreeze();
            });

            Thread.Sleep(2000);
            et += 2000;
            var t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(System.Math.Abs(et - t) < ToleranceInMillis);

            Thread.Sleep(5000);
            et += 4000;
            t = clock.Time;
            Debug.WriteLine($"t = {t}");
            Assert.IsTrue(System.Math.Abs(et - t) < ToleranceInMillis);
        }

    }

}
