using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MarukoLib.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class ArrayQueryTests
    {

        [TestMethod]
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public void Test()
        {
            var result = new ArrayQuery("1:100").Enumerate().ToArray();
            Assert.IsTrue(result.Length == 100 && result[0] == 1 && result[99] == 100);
            result = new ArrayQuery("start:end").Enumerate(5, 8).ToArray();
            Assert.IsTrue(result.Length == 4 && result[0] == 5 && result[3] == 8);
            result = new ArrayQuery(":").Enumerate(5, 8).ToArray();
            Assert.IsTrue(result.Length == 4 && result[0] == 5 && result[3] == 8);
            result = new ArrayQuery("1:end").Enumerate(1, 2).ToArray();
            Assert.IsTrue(result.Length == 2 && result[0] == 1 && result[1] == 2);
            result = new ArrayQuery("1:end , 8").Enumerate(1, 10).ToArray();
            Assert.IsTrue(result.Length == 11 && result[0] == 1 && result[10] == 8);
        }
        
    }

}
