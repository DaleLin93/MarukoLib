using System;
using MarukoLib.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class InteropTests
    {

        [TestMethod]
        public void TestEnumWindows()
        {
            foreach (var hWnd in User32.EnumWindows())
                Console.WriteLine($"#{hWnd}. {User32.GetWindowText(hWnd)} ({User32.GetWindowRect(hWnd)})");
        }

    }

}
