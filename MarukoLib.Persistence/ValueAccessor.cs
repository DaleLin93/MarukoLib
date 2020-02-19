using System;
using System.Collections.Generic;
using System.Windows;

namespace MarukoLib.Persistence
{

    public interface IValueAccessor
    {

        Type ValueType { get; }

        object Value { get; set; }

    }

    public interface IValueAccessor<T>
    {

        T Value { get; set; }

    }

    public class DependencyPropertyAccessor : IValueAccessor
    {

        public DependencyPropertyAccessor(DependencyObject @object, DependencyProperty property)
        {
            Object = @object;
            Property = property;
        }

        public DependencyObject Object { get; }

        public DependencyProperty Property { get; }

        public Type ValueType => Property.PropertyType;

        public object Value { get => Object.GetValue(Property); set => Object.SetValue(Property, value); }

    }

    public sealed class DefaultAccessor<T> : IValueAccessor<T>, IValueAccessor 
    {

        public static readonly DefaultAccessor<T> Instance = new DefaultAccessor<T>();

        private DefaultAccessor() { }

        public Type ValueType => typeof(T);

        public T Value { get => default; set { } }

        object IValueAccessor.Value { get => default; set { } }

    }

    public abstract class ValueAccessor<T> : IValueAccessor<T>, IValueAccessor
    {

        public Type ValueType => typeof(T);

        public abstract T Value { get; set; }

        object IValueAccessor.Value { get => Value; set => Value = (T) value; }

    }

    public class DictionaryAccessor : ValueAccessor<IReadOnlyDictionary<string, object>>
    {

        private readonly IReadOnlyDictionary<string, IValueAccessor> _accessors;

        public DictionaryAccessor(IReadOnlyDictionary<string, IValueAccessor> accessors) => _accessors = accessors;

        public override IReadOnlyDictionary<string, object> Value
        {
            get
            {
                var dict = new Dictionary<string, object>();
                foreach (var entry in _accessors)
                    dict[entry.Key] = entry.Value.Value;
                return dict;
            }
            set
            {
                foreach (var entry in _accessors)
                    if (value.TryGetValue(entry.Key, out var val))
                        entry.Value.Value = val;
            }
        }

    }

}
