using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MarukoLib.LabStreamingLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class LabStreamingLayerTests
    {

        [TestMethod]
        public void Test()
        {
            const string deviceName = "BioSemi";
            const int channelNum = 8;
            var rnd = new Random();

            // create stream info and outlet
            var info = new LabStreamingLayerLib.StreamInfo(deviceName, "EEG", channelNum, 100, LabStreamingLayerLib.channel_format_t.cf_float32, "sddsfsdf");
            var outlet = new LabStreamingLayerLib.StreamOutlet(info);

            // wait until an EEG stream shows up
            var results = LabStreamingLayerLib.resolve_stream("type", "EEG");

            // open an inlet and print meta-data
            var inlet = new LabStreamingLayerLib.StreamInlet(results[0]);
            inlet.open_stream();
            Assert.AreEqual(deviceName, inlet.info().name());
            Assert.AreEqual(channelNum, inlet.info().channel_count());
            Debug.Write(inlet.info().as_xml());

            Assert.IsTrue(outlet.have_consumers());

            // send data, 8 channels
            var data = new float[8];
            for (var c = 0; c < 8; c++)
                data[c] = rnd.Next(-100, 100);
            outlet.push_sample(data);

            Thread.Sleep(1000);

            // read samples
            var sample = new float[inlet.info().channel_count()];
            Assert.AreEqual(1, inlet.samples_available());
            inlet.pull_sample(sample, 10);
            foreach (var f in sample)
                Debug.Write($"\t{f}");

            Assert.IsTrue(data.SequenceEqual(sample));
        }

    }

}
