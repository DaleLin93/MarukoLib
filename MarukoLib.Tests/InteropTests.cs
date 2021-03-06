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

        [TestMethod]
        public void TestEnumMonitors()
        {
            var monitorInfo = new User32.MonitorInfo();
            foreach (var hMonitor in User32.EnumMonitors())
            {
                if (User32.GetMonitorInfo(hMonitor, ref monitorInfo)) 
                    Console.WriteLine($"#{hMonitor}. {monitorInfo.monitor} ({monitorInfo.size})");
            }
        }

    }

}
