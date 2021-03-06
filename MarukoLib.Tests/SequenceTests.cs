using System;
using MarukoLib.Lang.Sequence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class SequenceTests
    {

        [TestMethod]
        public void PseudoRandomTest()
        {
            var random = new Random();
            for (var i = 0; i < 100; i++)
            {
                var targetCount = random.Next(1, 10);
                var counts = new int[targetCount];
                var sum = 0;
                for (var j = 0; j < targetCount; j++)
                    sum += counts[j] = random.Next(10, 1000);
                var targets = new RandomTarget<int>[targetCount];
                for (var j = 0; j < targetCount; j++)
                    targets[j] = new RandomTarget<int>(j + 1, counts[j] / (decimal)sum);
                var pseudoRandom = new PseduoRandomSequence<int>(targets);
                for (; sum > 0; sum--)
                {
                    var index = pseudoRandom.Next();
                    Assert.IsTrue((targetCount == 0) == (index == 0));
                    if (index > 0) counts[index - 1]--;
                }
                for (var j = 0; j < targetCount; j++)
                    Assert.AreEqual(0, counts[j]);
            }
        }

        [TestMethod]
        public void PseudoRandomBoolsTest()
        {
            var random = new Random();
            for (var i = 0; i < 100; i++)
            {
                var trueCount = random.Next(10, 100);
                var totalCount = random.Next(trueCount, 500);
                var targetRate = (decimal)trueCount / totalCount;
                var pseudoRandom = new PseudoRandomBools(targetRate);
                for (; totalCount > 0; totalCount--)
                    if (pseudoRandom.Next()) trueCount--;
                Assert.AreEqual(0, trueCount);
                Assert.AreEqual(0, totalCount);
            }
        }

    }

}
