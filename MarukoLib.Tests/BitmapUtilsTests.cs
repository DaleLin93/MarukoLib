using System.Drawing;
using MarukoLib.Graphics;
using MarukoLib.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class BitmapUtilsTests
    {

        [TestMethod]
        public void TestScales()
        {
            var obama = Resources.Obama;
            obama.Scale(new Size(500, 500), ScaleMode.ScaleToFill, 1);
            obama.Scale(new Size(500, 500), ScaleMode.ScaleAspectFit, 1);
            obama.Scale(new Size(500, 500), ScaleMode.ScaleAspectFill, 1);
        }

    }
}
