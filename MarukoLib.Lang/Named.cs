using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarukoLib.Lang
{

    public interface INamed
    {

        string Name { get; }

    }

    public interface INamedObject : INamed
    {

        object Value { get; }

    }

    public class NamedObject : INamedObject
    {

        public NamedObject(KeyValuePair<string, object> pair) : this(pair.Key, pair.Value) { }

        public NamedObject(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public object Value { get; }

        public static NamedObject[] Of<T>(IReadOnlyCollection<T> values, Func<T, string> toStringFunc)
        {
            var namedValues = new NamedObject[values.Count];
            var i = 0;
            foreach (var value in values) namedValues[i++] = new NamedObject(toStringFunc(value), value);
            return namedValues;
        }

        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj) => obj is INamedObject that && Equals(that.Name, Name) && Equals(that.Value, Value);

        public override string ToString() => Name;

    }

    public class Named<T> : NamedObject
    {

        public Named(KeyValuePair<string, T> pair) : this(pair.Key, pair.Value) { }

        public Named(string name, T value) : base(name, value) { }

        public new T Value => (T) base.Value;

        public static Named<T>[] Of(IReadOnlyCollection<T> values, Func<T, string> toStringFunc)
        {
            var namedValues = new Named<T>[values.Count];
            var i = 0;
            foreach (var value in values) namedValues[i++] = new Named<T>(toStringFunc(value), value);
            return namedValues;
        }

    }

}
