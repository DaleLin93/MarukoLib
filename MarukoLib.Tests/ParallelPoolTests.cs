using System.Linq;
using System.Threading;
using MarukoLib.Lang.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class ParallelPoolTests
    {

        private static void DoCompute() => Thread.Sleep(2000);

        [TestMethod]
        public void TestParallelPool()
        {
            var count = ParallelPool.MaxParallelLevel * ParallelPool.MaxParallelLevel + 1;
            var parallelPool = new ParallelPool(ParallelPool.MaxParallelLevel);
            var completed = new bool[count];
            parallelPool.Batch(task =>
            {
                for (var i = task.TaskIndex; i < count; i+=parallelPool.ParallelLevel)
                {
                    DoCompute();
                    completed[i] = true;
                }
            });
            Assert.IsTrue(completed.All(v => v));

            completed = new bool[count];
            parallelPool.Batch((uint) completed.Length, task =>
            {
                DoCompute();
                completed[task.TaskIndex] = true;
            });
            Assert.IsTrue(completed.All(v => v));
        }

    }

}
