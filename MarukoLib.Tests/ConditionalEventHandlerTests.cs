using System;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using MarukoLib.Lang.Concurrent;
using MarukoLib.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class ConditionalEventHandlerTests
    {

        public class Foo
        {

            public event EventHandler<string> Bar;

            public void FooBar(string text) => Bar?.Invoke(this, text);

        }

        public class ConditionalEventHandler : ConditionalEventHandler<Foo>
        {

            private readonly EventHandler<string> _eventHandler;

            public ConditionalEventHandler(EventHandler<string> actualHandler) => _eventHandler = actualHandler;

            protected override void PostAttach(Foo foo) => foo.Bar += Foo_OnBar;

            protected override void PostDetach(Foo foo) => foo.Bar -= Foo_OnBar;

            private void Foo_OnBar(object sender, string e)
            {
                if (!IsBlocked) _eventHandler.Invoke(sender, e);
            }
        }

        [TestMethod]
        public void Test()
        {
            const string testString = @"AVCRGHQSFAADWF";
            var r = new Random();
            var stringBuilder = new StringBuilder();
            var array = new[] { new Foo(), new Foo(), new Foo() };
            using (var ceh = new ConditionalEventHandler((sender, args) => stringBuilder.Append(args)))
            {
                foreach (var foo in array) ceh.Attach(foo);
                for (var i = 0; i < 10; i++)
                {
                    var block = r.NextDouble() < 0.5;
                    var disposable = block ? ceh.Block() : null;
                    var pos = 0;
                    while (pos < testString.Length)
                    {
                        var idx = r.Next(0, array.Length);
                        var length = Math.Max(r.Next(0, testString.Length - pos), 1);
                        array[idx].FooBar(testString.Substring(pos, length));
                        pos += length;
                    }
                    Assert.AreEqual(block ? string.Empty : testString, stringBuilder.ToString());
                    stringBuilder.Clear();
                    disposable?.Dispose();
                }
            }
        }

        [TestMethod]
        public void TestUi()
        {
            var r = new Random();
            var counter = Atomics.Int();
            var textBox = new TextBox();
            using (var ceh = ConditionalEventHandlers.TextBoxOnTextChanged((sender, args) => counter.IncrementAndGet()))
            {
                ceh.Attach(textBox);
                var count = 0;
                for (var i = 0; i < 100; i++)
                {
                    var block = r.NextDouble() < 0.5;
                    if (!block) count++;
                    var disposable = block ? ceh.Block() : null;
                    while (true)
                    {
                        var text = r.NextDouble().ToString(CultureInfo.InvariantCulture);
                        if (Equals(text, textBox.Text)) continue;
                        textBox.Text = text;
                        break;
                    }
                    disposable?.Dispose();
                    Assert.AreEqual(count, counter.Get());
                }
            }
        }

    }
}
