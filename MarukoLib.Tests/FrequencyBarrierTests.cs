using System.Diagnostics;
using MarukoLib.Lang.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class FrequencyBarrierTests
    {

        [TestMethod]
        public void Test()
        {
            const int repeatTimes = 4;
            var stopwatch = new Stopwatch();
            var frequencyBarrier = FrequencyBarrier.MinIntervalSecs(1.111);

            stopwatch.Start();
            for (var i = 0; i < repeatTimes; i++)
                frequencyBarrier.WaitOne();
            stopwatch.Stop();

            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds >= repeatTimes - 1);
        }

    }

}
