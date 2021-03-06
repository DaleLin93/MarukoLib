using MarukoLib.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class StringUtilsTests
    {

        [TestMethod]
        public void TestTrim()
        {
            Assert.AreEqual("--", "AA--".TrimStart("A"));
            Assert.AreEqual("A--", "AA--".TrimStart("A", false));
            Assert.AreEqual("AA-", "AA--".TrimEnd("-", false));
            Assert.AreEqual("AA", "AA--".TrimEnd("-"));
            Assert.AreEqual("BB", "AABBCC".Trim("A", "C"));
            Assert.AreEqual("ABBC", "AABBCC".Trim("A", "C", false));
        }

        [TestMethod]
        public void TestTrimOrPad()
        {
            Assert.AreEqual("AA--", "AA".TrimOrPadRight(4, '-'));
            Assert.AreEqual("AAAA", "AAAA".TrimOrPadRight(4, '-'));
            Assert.AreEqual("AAAA", "AAAAAA".TrimOrPadRight(4, '-'));
            Assert.AreEqual("----", StringUtils.TrimOrPadRight(null, 4, '-'));
        }

    }

}
