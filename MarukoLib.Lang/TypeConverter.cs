using System;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public interface ITypeConverter
    {

        Type InputType { get; }

        Type OutputType { get; }

        object ConvertForward(object input);

        object ConvertBackward(object input);

        ITypeConverter Inverse();

    }

    public interface ITypeConverter<TI, TO> : ITypeConverter
    {

        TO ConvertForward(TI input);

        TI ConvertBackward(TO input);

        new ITypeConverter<TO, TI> Inverse();

    }

    public sealed class TypeConverter : ITypeConverter
    {

        public delegate object ConvertFunc(object input);

        public static readonly TypeConverter Identity = Of<object, object>(Functions.Identity, Functions.Identity);

        private readonly ConvertFunc _forward;

        private readonly ConvertFunc _backward;

        private TypeConverter _inverted;

        private TypeConverter(Type inputType, Type outputType, ConvertFunc forward, ConvertFunc backward, TypeConverter inverted)
        {
            _forward = forward;
            _backward = backward;
            InputType = inputType;
            OutputType = outputType;
            _inverted = inverted;
        }

        public static TypeConverter Of<TIn, TOut>([NotNull] Func<TIn, TOut> forward, [NotNull] Func<TOut, TIn> backward) =>
            new TypeConverter(typeof(TIn), typeof(TOut), val => forward((TIn)val), val => backward((TOut)val), null);

        public static TypeConverter OfNull2Null<TIn, TOut>([NotNull] Func<TIn, TOut> forward, [NotNull] Func<TOut, TIn> backward) where TIn : class where TOut : class =>
            new TypeConverter(typeof(TIn), typeof(TOut), val => val == null ? null : forward((TIn)val), val => val == null ? null : backward((TOut)val), null);

        public Type InputType { get; }

        public Type OutputType { get; }

        public ITypeConverter Inverse() => _inverted ?? (_inverted = new TypeConverter(OutputType, InputType, _backward, _forward, this));

        public object ConvertForward(object input) => _forward(input);

        public object ConvertBackward(object input) => _backward(input);

    }

    public sealed class TypeConverter<T1, T2> : ITypeConverter<T1, T2>
    {

        private readonly Func<T1, T2> _forward;

        private readonly Func<T2, T1> _backward;

        private TypeConverter<T2, T1> _inverted;

        private TypeConverter(Func<T1, T2> forward, Func<T2, T1> backward, TypeConverter<T2, T1> inverted)
        {
            _forward = forward;
            _backward = backward;
            _inverted = inverted;
        }

        public static TypeConverter<T1, T2> Of([NotNull] Func<T1, T2> forward, [NotNull] Func<T2, T1> backward) => new TypeConverter<T1, T2>(val => forward(val), val => backward(val), null);

        public Type InputType => typeof(T1);

        public Type OutputType => typeof(T2);

        public bool IsExactlyMatch(Type inputType, Type outputType) => InputType == inputType && OutputType == outputType;

        public bool IsMatch(Type inputType, Type outputType) => InputType.IsAssignableFrom(inputType) && outputType.IsAssignableFrom(OutputType);

        public ITypeConverter<T2, T1> Inverse() => _inverted ?? (_inverted = new TypeConverter<T2, T1>(_backward, _forward, this));

        public T2 ConvertForward(T1 input) => _forward(input);

        public T1 ConvertBackward(T2 input) => _backward(input);

        object ITypeConverter.ConvertForward(object input) => ConvertForward((T1)input);

        object ITypeConverter.ConvertBackward(object input) => ConvertBackward((T2)input);

        ITypeConverter ITypeConverter.Inverse() => Inverse();

    }

    public static class IdentityTypeConverter<T>
    {

        public static readonly ITypeConverter<T, T> Instance = TypeConverter<T, T>.Of(Functions.Identity, Functions.Identity);

    }

    public static class TypeConverterExt
    {

        public static bool IsExactlyMatch(this ITypeConverter typeConverter, Type inputType, Type outputType) => 
            typeConverter.InputType == inputType && typeConverter.OutputType == outputType;

        public static bool IsMatch(this ITypeConverter typeConverter, Type inputType, Type outputType) =>
            (inputType == null || typeConverter.InputType.IsAssignableFrom(inputType)) && (outputType == null || outputType.IsAssignableFrom(typeConverter.OutputType));

        public static ITypeConverter<T1, T2> OfNull2Null<T1, T2>([NotNull] Func<T1, T2> forward, [NotNull] Func<T2, T1> backward) where T1 : class where T2 : class =>
            TypeConverter<T1, T2>.Of(val => val == null ? null : forward(val), val => val == null ? null : backward(val));

        public static ITypeConverter<T1, T2> As<T1, T2>(this ITypeConverter typeConverter)
        {
            if (!typeConverter.IsExactlyMatch(typeof(T1), typeof(T2))) throw new ArgumentException("type not match");
            if (typeConverter is ITypeConverter<T1, T2> typed) return typed;
            return TypeConverter<T1, T2>.Of(val => (T2)typeConverter.ConvertForward(val), val => (T1)typeConverter.ConvertBackward(val));
        }

    }

}
