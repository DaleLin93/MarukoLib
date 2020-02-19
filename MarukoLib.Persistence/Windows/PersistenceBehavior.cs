using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MarukoLib.Persistence.Windows
{
    public static class PersistenceBehavior
    {

        public class DependencyObjectAccessResolver : IAccessResolver
        {

            public class Accessor : IValueAccessor
            {

                private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

                private readonly IValueAccessor _internalAccessor;

                internal Accessor(IValueAccessor internalAccessor) => _internalAccessor = internalAccessor;

                public Type ValueType => _internalAccessor.ValueType;

                public object Value { get => _internalAccessor.Value; set => _internalAccessor.Value = value; }

                public string Serialize()
                {
                    lock (_serializer)
                        return _serializer.Serialize(Value);
                }

                public void Deserialize(string str)
                {
                    object dict;
                    lock (_serializer)
                        dict = _serializer.DeserializeObject(str);
                    Value = dict;
                }

                public void SerializeToFile(string path) => File.WriteAllText(path, Serialize(), Encoding.UTF8);

                public void DeserializeFromFile(string path) => Deserialize(File.ReadAllText(path, Encoding.UTF8));

            }

            protected class Resolver0 : IAccessResolver
            {

                private readonly Func<DependencyObject, IValueAccessor> _resolve;

                private Resolver0(Type baseType, Func<DependencyObject, IValueAccessor> resolve)
                {
                    BaseType = baseType;
                    _resolve = resolve;
                }

                public static Resolver0 Create<T>(Func<T, IValueAccessor> resolve) where T : DependencyObject
                    => new Resolver0(typeof(T), obj => resolve((T)obj));

                public Type BaseType { get; }

                public IValueAccessor Resolve(object obj) => _resolve((DependencyObject)obj);

            }

            internal static readonly DependencyObjectAccessResolver Instance = new DependencyObjectAccessResolver();

            protected DependencyObjectAccessResolver() { }

            protected static readonly IReadOnlyCollection<Resolver0> Resolvers = new[]
            {
                Resolver0.Create<TextBox>(textBox => new DependencyPropertyAccessor(textBox, TextBox.TextProperty)),
                Resolver0.Create<CheckBox>(checkBox => new DependencyPropertyAccessor(checkBox, ToggleButton.IsCheckedProperty)),
            };

            protected static void Resolve(Dictionary<string, IValueAccessor> output, object value)
            {
                if (value == null) return;
                if (value is DependencyObject obj)
                {
                    var key = GetKey(obj);
                    if (key != null)
                        foreach (var resolver in Resolvers)
                            if (resolver.BaseType.IsInstanceOfType(obj))
                            {
                                output[key] = resolver.Resolve(obj);
                                return;
                            }

                    IEnumerable children;
                    if (obj is Decorator decorator)
                        children = new[] { decorator.Child };
                    else if (obj is ContentControl contentControl)
                        children = new[] { contentControl.Content };
                    else if (obj is Panel panel)
                        children = panel.Children;
                    else if (key != null)
                        throw new NotSupportedException();
                    else
                        return;
                    var dict = key == null ? output : new Dictionary<string, IValueAccessor>();
                    foreach (var child in children)
                        Resolve(dict, child);
                    if (key != null) output[key] = new DictionaryAccessor(dict);
                }
            }

            public Accessor Resolve(object obj)
            {
                if (!(obj is DependencyObject)) throw new ArgumentException($"Input value must be a DependencyObject.");
                var dict = new Dictionary<string, IValueAccessor>();
                Resolve(dict, obj);
                return new Accessor(new DictionaryAccessor(dict));
            }

            IValueAccessor IAccessResolver.Resolve(object obj) => Resolve(obj);

        }

        public static readonly DependencyObjectAccessResolver DefaultAccessResolver = DependencyObjectAccessResolver.Instance;

        public static readonly DependencyProperty KeyProperty = DependencyProperty
            .RegisterAttached("Key", typeof(string), typeof(PersistenceBehavior), new PropertyMetadata(null, null));

        public static string GetKey(DependencyObject obj) => (string)obj.GetValue(KeyProperty);

        public static void SetKey(DependencyObject obj, string value) => obj.SetValue(KeyProperty, value);

    }
}
