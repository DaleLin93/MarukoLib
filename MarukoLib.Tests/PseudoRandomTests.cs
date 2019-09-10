using System;
using MarukoLib.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class PseudoRandomTests
    {

        [TestMethod]
        public void Test()
        {
            var random = new Random();
            for (var i = 0; i < 100; i++)
            {
                var trueCount = random.Next(10, 100);
                var totalCount = random.Next(trueCount, 500);
                var targetRate = (double) trueCount / totalCount;
                var pseudoRandom = new PseudoRandom(targetRate);
                for (; totalCount > 0; totalCount--)
                    if (pseudoRandom.Next()) trueCount--;
                Assert.AreEqual(0, trueCount);
                Assert.AreEqual(0, totalCount);
            }
        }

    }

}
