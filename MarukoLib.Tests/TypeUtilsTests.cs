using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MarukoLib.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class TypeUtilsTests
    {

        public interface IA<T> { }

        public interface IB<T> : IA<T> { }

        public interface IC : IB<string> { }

        public class D : IB<ushort>, IC { }

        [TestMethod]
        [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
        public void TestGetGenericArguments()
        {
            Type[] arguments;

            arguments = typeof(IA<>).GetGenericTypes(typeof(IA<>)).ToArray();
            Assert.IsTrue(new Type[] { null }.SequenceEqual(arguments));

            arguments = typeof(IB<>).GetGenericTypes(typeof(IA<>)).ToArray();
            Assert.IsTrue(new Type[] { null }.SequenceEqual(arguments));

            arguments = typeof(IB<long>).GetGenericTypes(typeof(IA<>)).ToArray();
            Assert.IsTrue(new[] { typeof(long) }.SequenceEqual(arguments));

            arguments = typeof(IC).GetGenericTypes(typeof(IA<>)).ToArray();
            Assert.IsTrue(new[] { typeof(string) }.SequenceEqual(arguments));

            arguments = typeof(D).GetGenericTypes(typeof(IA<>)).ToArray();
            Assert.IsTrue(new[] { typeof(ushort), typeof(string) }.SequenceEqual(arguments));

        }

    }

}
