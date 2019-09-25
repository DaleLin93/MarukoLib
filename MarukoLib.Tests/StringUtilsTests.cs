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
            Assert.AreEqual("AA--", "AA".TrimOrPad(4, '-'));
            Assert.AreEqual("AAAA", "AAAA".TrimOrPad(4, '-'));
            Assert.AreEqual("AAAA", "AAAAAA".TrimOrPad(4, '-'));
            Assert.AreEqual("----", StringUtils.TrimOrPad(null, 4, '-'));
        }

    }

}
