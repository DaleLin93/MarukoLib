using JetBrains.Annotations;
using MarukoLib.Lang;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows;

namespace MarukoLib.Persistence
{

    public interface IValueAccessor
    {

        [NotNull] Type ValueType { get; }

        [CanBeNull] object Value { get; set; }

    }

    public interface IValueAccessor<T>
    {

        T Value { get; set; }

    }

    public class DependencyPropertyAccessor : IValueAccessor
    {

        public DependencyPropertyAccessor([NotNull] DependencyProperty property, [NotNull] DependencyObject @object)
        {
            Property = property;
            Object = @object;
        }

        [NotNull] public DependencyProperty Property { get; }

        [NotNull] public DependencyObject Object { get; }

        public Type ValueType => Property.PropertyType;

        public object Value { get => Object.GetValue(Property); set => Object.SetValue(Property, value); }

    }

    public class ReflectionFieldAccessor : IValueAccessor
    {

        public ReflectionFieldAccessor([NotNull] FieldInfo field, [CanBeNull] object invoker)
        {
            if (invoker == null && !field.IsStatic)
                throw new ArgumentNullException(nameof(invoker));
            Field = field;
            Invoker = invoker;
        }

        [NotNull] public FieldInfo Field { get; }

        [CanBeNull] public object Invoker { get; }

        public Type ValueType => Field.FieldType;

        public object Value { get => Field.GetValue(Invoker); set => Field.SetValue(Invoker, value); }

    }

    public class ReflectionMethodsAccessor : IValueAccessor
    {

        public ReflectionMethodsAccessor([NotNull] MethodInfo getMethod, [NotNull] MethodInfo setMethod, [CanBeNull] object invoker)
        {
            if (invoker == null && (!getMethod.IsStatic || !setMethod.IsStatic))
                throw new ArgumentNullException(nameof(Invoker));
            if (getMethod.GetParameters().Length != 0 || getMethod.ReturnType == typeof(void))
                throw new ArgumentException("Get method must have a non-void return type and without parameters.");
            if (setMethod.GetParameters().Length != 1)
                throw new ArgumentException("Set method must be a method with exactly 1 parameter.");
            ValueType = GetValueType(getMethod.ReturnType, setMethod.GetParameters()[0].ParameterType)
                ?? throw new ArgumentException("Types of get method and set method are not match.");
            GetMethod = getMethod;
            SetMethod = setMethod;
            Invoker = invoker;
        }

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        private static Type GetValueType(Type a, Type b)
        {
            if (a.IsAssignableFrom(b)) return a;
            if (b.IsAssignableFrom(a)) return b;
            return null;
        }

        [NotNull] public MethodInfo GetMethod { get; }

        [NotNull] public MethodInfo SetMethod { get; }

        [CanBeNull] public object Invoker { get; }

        public Type ValueType { get; }

        public object Value
        {
            get => GetMethod.Invoke(Invoker, EmptyArray<object>.Instance);
            set => SetMethod.Invoke(Invoker, new[] { value });
        }

    }

    public class ReflectionPropertyAccessor : ReflectionMethodsAccessor
    {

        public ReflectionPropertyAccessor([NotNull] PropertyInfo property, [CanBeNull] object invoker)
            : base(property.GetMethod, property.SetMethod, invoker) => Property = property;

        [NotNull] public PropertyInfo Property { get; }

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

    public class DelegatedAccessor<T> : ValueAccessor<T>
    {

        public DelegatedAccessor([NotNull] Func<T> getFunction, [NotNull] Action<T> setAction)
        {
            GetFunction = getFunction ?? throw new ArgumentNullException(nameof(getFunction));
            SetAction = setAction ?? throw new ArgumentNullException(nameof(setAction));
        }

        [NotNull] public Func<T> GetFunction { get; }

        [NotNull] public Action<T> SetAction { get; }

        public override T Value { get => GetFunction(); set => SetAction(value); }

    }

    public class ArrayAccessor : ValueAccessor<object[]>
    {

        [NotNull] private readonly IValueAccessor[] _accessors;

        public ArrayAccessor([NotNull] IValueAccessor[] accessors) => _accessors = accessors;

        public override object[] Value
        {
            get
            {
                var array = new object[_accessors.Length];
                for (var i = 0; i < _accessors.Length; i++)
                    array[i] = _accessors[i].Value;
                return array;
            }
            set
            {
                if (value == null) return;
                var count = Math.Min(value.Length, _accessors.Length);
                for (var i = 0; i < count; i++)
                    _accessors[i].Value = value[i];
            }
        }

    }

    public class DictionaryAccessor : ValueAccessor<IReadOnlyDictionary<string, object>>
    {

        [NotNull] private readonly IReadOnlyDictionary<string, IValueAccessor> _accessors;

        public DictionaryAccessor([NotNull] IReadOnlyDictionary<string, IValueAccessor> accessors) => _accessors = accessors;

        [NotNull]
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
