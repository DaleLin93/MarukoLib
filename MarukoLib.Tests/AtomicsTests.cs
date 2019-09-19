using System;
using System.Threading;
using MarukoLib.Lang.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{
    [TestClass]
    public class AtomicsTests
    {

        private class A 
        {

            public readonly int B;

            public A(int b) => B = b;

            public override int GetHashCode() => B;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && ((A)obj).B == B;
            }

            public override string ToString() => $"{nameof(B)}: {B}";

        }

        [TestMethod]
        public void TestAtomicBool()
        {
            var atomicBool = new AtomicBool(false);
            Assert.IsFalse(atomicBool.Reset());
            Assert.IsTrue(atomicBool.Set());
            Assert.IsFalse(atomicBool.Set());
            Assert.IsTrue(atomicBool.Reset());
            Assert.IsFalse(atomicBool.Get());
            Assert.IsFalse(atomicBool.CompareAndSet(true, false));
            Assert.IsTrue(atomicBool.CompareAndSet(false, true));
        }

        [TestMethod]
        public void TestAtomicInt()
        {
            var atomicInt = new AtomicInt(0);
            Assert.AreEqual(0, atomicInt.Get());
            Assert.AreEqual(1, atomicInt.IncrementAndGet());
            Assert.AreEqual(1, atomicInt.GetAndIncrement());
            Assert.AreEqual(2, atomicInt.Get());
            Assert.IsFalse(atomicInt.CompareAndSet(1, 5));
            Assert.AreEqual(2, atomicInt.Get());
            Assert.IsTrue(atomicInt.CompareAndSet(2, 5));
            Assert.AreEqual(5, atomicInt.Get());
            Assert.AreEqual(5, atomicInt.Set(10));
            Assert.AreEqual(10, atomicInt.Get());

            atomicInt = new AtomicInt(100);

            const int threadNum = 4;
            const int loopCount = 100;
            var countdownLatch = new CountDownLatch(threadNum);
            for (var i = 0; i < threadNum; i++)
            {
                new Thread(() =>
                {
                    for (var j = 0; j < loopCount; j++)
                        atomicInt.IncrementAndGet();
                    countdownLatch.CountDown();
                }).Start();
            }
            countdownLatch.Await();
            Assert.AreEqual(100 + threadNum * loopCount, atomicInt.Get());
        }

        [TestMethod]
        public void TestAtomicRef()
        {
            var atomicRef = new Atomic<A>();
            Assert.AreEqual(null, atomicRef.Reference);
            Assert.AreEqual(null, atomicRef.Set(new A(1)));
            Assert.AreEqual(new A(1), atomicRef.Reference);
            Assert.IsFalse(atomicRef.CompareAndSet(new A(1), new A(2)));
            Assert.AreEqual(new A(1), atomicRef.Reference);
            Assert.IsTrue(atomicRef.CompareAndSet(atomicRef.Reference, new A(2)));
            Assert.AreEqual(new A(2), atomicRef.Reference);
        }


    }
}
