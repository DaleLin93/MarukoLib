using System;
using System.Collections;
using System.Linq;

namespace MarukoLib.Lang
{

    public sealed class ToStringOverridenWrapper
    {

        public readonly object Value;

        private readonly Func<object, string> _toStringFunc;

        private ToStringOverridenWrapper(object value, Func<object, string> toStringFunc)
        {
            Value = value;
            _toStringFunc = toStringFunc;
        }

        public static object TryUnwrap(object obj) => obj is ToStringOverridenWrapper wrapper ? wrapper.Value : obj;

        public static ToStringOverridenWrapper Wrap(object value, Func<object, string> toStringFunc) => value is ToStringOverridenWrapper wrapper 
            ? wrapper._toStringFunc == toStringFunc ? wrapper : new ToStringOverridenWrapper(wrapper.Value, toStringFunc)
            : new ToStringOverridenWrapper(value, toStringFunc);

        public static ToStringOverridenWrapper[] Of(IEnumerable values, Func<object, string> toStringFunc) =>
            values.OfType<object>().Select(value => Wrap(value, toStringFunc)).ToArray();

        public bool Equals(ToStringOverridenWrapper other) => Equals(Value, other.Value);

        public override bool Equals(object obj)
        {
            if (null == obj) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ToStringOverridenWrapper)obj);
        }

        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;

        public override string ToString() => _toStringFunc(Value);

    }

}
