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

        public class Serializable : IValueAccessor
        {

            private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

            private readonly IValueAccessor _internalAccessor;

            internal Serializable(IValueAccessor internalAccessor) => _internalAccessor = internalAccessor;

            public Type ValueType => _internalAccessor.ValueType;

            public string Serialize()
            {
                lock (_serializer)
                    return _serializer.Serialize(_internalAccessor.Value);
            }

            public void Deserialize(string str)
            {
                object dict;
                lock (_serializer)
                    dict = _serializer.DeserializeObject(str);
                _internalAccessor.Value = dict;
            }

            public void SerializeToFile(string path) => File.WriteAllText(path, Serialize(), Encoding.UTF8);

            public void DeserializeFromFile(string path) => Deserialize(File.ReadAllText(path, Encoding.UTF8));

            object IValueAccessor.Value { get => _internalAccessor.Value; set => _internalAccessor.Value = value; }

        }

        public class DependencyObjectAccessResolver : IAccessResolver
        {

            protected class InternalResolver : IAccessResolver
            {

                private readonly Func<DependencyObject, IValueAccessor> _resolve;

                private InternalResolver(Type baseType, Func<DependencyObject, IValueAccessor> resolve)
                {
                    BaseType = baseType;
                    _resolve = resolve;
                }

                public static InternalResolver Create<T>(Func<T, IValueAccessor> resolve) where T : DependencyObject
                    => new InternalResolver(typeof(T), obj => resolve((T)obj));

                public Type BaseType { get; }

                public IValueAccessor Resolve(object obj) => _resolve((DependencyObject)obj);

            }

            internal static readonly DependencyObjectAccessResolver Instance = new DependencyObjectAccessResolver();

            protected DependencyObjectAccessResolver() { }

            protected static readonly IReadOnlyCollection<InternalResolver> Resolvers = new[]
            {
                InternalResolver.Create<TextBox>(textBox => new DependencyPropertyAccessor(TextBox.TextProperty, textBox)),
                InternalResolver.Create<CheckBox>(checkBox => new DependencyPropertyAccessor(ToggleButton.IsCheckedProperty, checkBox))
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

            public Serializable Resolve(object obj)
            {
                if (!(obj is DependencyObject)) throw new ArgumentException("Input value must be a DependencyObject.");
                var dict = new Dictionary<string, IValueAccessor>();
                Resolve(dict, obj);
                return new Serializable(new DictionaryAccessor(dict));
            }

            IValueAccessor IAccessResolver.Resolve(object obj) => Resolve(obj);

        }

        public static readonly DependencyProperty KeyProperty = DependencyProperty
            .RegisterAttached("Key", typeof(string), typeof(PersistenceBehavior), new PropertyMetadata(null, null));

        public static string GetKey(DependencyObject obj) => (string)obj.GetValue(KeyProperty);

        public static void SetKey(DependencyObject obj, string value) => obj.SetValue(KeyProperty, value);

        public static Serializable AsSerializable(DependencyObject @object) => DependencyObjectAccessResolver.Instance.Resolve(@object);

    }
}
