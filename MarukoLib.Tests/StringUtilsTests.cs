using MarukoLib.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class StringUtilsTests
    {

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
