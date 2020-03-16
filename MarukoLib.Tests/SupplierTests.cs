using System;
using System.Threading;
using MarukoLib.Lang;
using MarukoLib.Lang.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class SupplierTests
    {

        [TestMethod]
        public void Test()
        {
            var atomicInt = Atomics.Int();
            Supplier<int> baseSupplier = () => atomicInt.IncrementAndGet();

            var memoizingSupplier = baseSupplier.Memoize();
            var memoizingWithExpirationSupplier = baseSupplier.MemoizeWithExpiration(TimeSpan.FromSeconds(5));

            Assert.AreEqual(1, baseSupplier());
            Assert.AreEqual(2, baseSupplier());
            Assert.AreEqual(3, memoizingSupplier());
            Assert.AreEqual(3, memoizingSupplier());
            Assert.AreEqual(4, memoizingWithExpirationSupplier());
            Assert.AreEqual(4, memoizingWithExpirationSupplier());
            Thread.Sleep(TimeSpan.FromSeconds(3));
            Assert.AreEqual(4, memoizingWithExpirationSupplier());
            Thread.Sleep(TimeSpan.FromSeconds(3));
            Assert.AreEqual(5, memoizingWithExpirationSupplier());
            Assert.AreEqual(3, memoizingSupplier());
            Assert.AreEqual(5, atomicInt.Value);
            Assert.AreEqual(6, baseSupplier());
        } 

    }

}
