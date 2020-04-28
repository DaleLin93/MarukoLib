using System;
using JetBrains.Annotations;
using MarukoLib.Lang.Exceptions;

namespace MarukoLib.Lang
{

    public interface ITypeConverter
    {

        [NotNull] Type InputType { get; }

        [NotNull] Type OutputType { get; }

        object ConvertForward(object input);

        object ConvertBackward(object input);

        [NotNull] ITypeConverter Inverse();

    }

    public interface ITypeConverter<TI, TO> : ITypeConverter
    {

        TO ConvertForward(TI input);

        TI ConvertBackward(TO input);

        new ITypeConverter<TO, TI> Inverse();

    }

    public abstract class TypeConverter : ITypeConverter
    {

        internal sealed class InvertedConverter : TypeConverter
        {

            public InvertedConverter([NotNull] ITypeConverter converter) 
                : base(converter.OutputType, converter.InputType, (converter as TypeConverter)?.TypeCheck ?? true, converter ?? throw new ArgumentNullException(nameof(converter))) { }

            protected override object InternalConvertForward(object input) => Inverse().ConvertBackward(input);

            protected override object InternalConvertBackward(object input) => Inverse().ConvertForward(input);

        }

        internal sealed class FuncConverter : TypeConverter
        {

            [NotNull] private readonly Func<object, object> _forward;

            [NotNull] private readonly Func<object, object> _backward;

            public FuncConverter([NotNull] Type inputType, [NotNull] Type outputType, [NotNull] Func<object, object> forward, [NotNull] Func<object, object> backward)
                : this(inputType, outputType, forward, backward, true, null) { }

            internal FuncConverter([NotNull] Type inputType, [NotNull] Type outputType, [NotNull] Func<object, object> forward, [NotNull] Func<object, object> backward, 
                bool typeCheck, [CanBeNull] ITypeConverter inverted) : base(inputType, outputType, typeCheck, inverted)
            {
                _forward = forward;
                _backward = backward;
            }

            protected override object InternalConvertForward(object input) => _forward(input);

            protected override object InternalConvertBackward(object input) => _backward(input);

            protected override ITypeConverter CreateInverted() => new FuncConverter(OutputType, InputType, _backward, _forward, TypeCheck, this);

        }

        internal sealed class ConvertConverter : TypeConverter
        {

            internal ConvertConverter([NotNull] Type inputType, [NotNull] Type outputType, [CanBeNull] ITypeConverter inverted) 
                : base(inputType, outputType, false, inverted) { }

            protected override object InternalConvertForward(object input) => Convert.ChangeType(input, OutputType);

            protected override object InternalConvertBackward(object input) => Convert.ChangeType(input, InputType);

            protected override ITypeConverter CreateInverted() => new ConvertConverter(OutputType, InputType, this);

        }

        [CanBeNull] private ITypeConverter _inverted;

        internal TypeConverter([NotNull] Type inputType, [NotNull] Type outputType, bool typeCheck, [CanBeNull] ITypeConverter inverted)
        {
            InputType = inputType;
            OutputType = outputType;
            TypeCheck = typeCheck;
            _inverted = inverted;
        }

        public static ITypeConverter Of([NotNull] Type inputType, [NotNull] Type outputType,
            [NotNull] Func<object, object> forward, [NotNull] Func<object, object> backward)
            => new FuncConverter(inputType, outputType, forward, backward, true, null);

        public static ITypeConverter OfNull2Null([NotNull] Type inputType, [NotNull] Type outputType,
            [NotNull] Func<object, object> forward, [NotNull] Func<object, object> backward) 
            => new FuncConverter(inputType, outputType, val => val == null ? null : forward(val), val => val == null ? null : backward(val), false, null);

        public static ITypeConverter Identity(Type inputType, Type outputType) => new FuncConverter(inputType, outputType, Functions.Identity, Functions.Identity, true, null);

        public static ITypeConverter SystemConvert(Type inputType, Type outputType) => new ConvertConverter(inputType, outputType, null);

        public static ITypeConverter Inverse([NotNull] ITypeConverter converter) => new InvertedConverter(converter);

        public Type InputType { get; }

        public Type OutputType { get; }

        protected bool TypeCheck { get; }

        public ITypeConverter Inverse() => _inverted ?? (_inverted = CreateInverted());

        public object ConvertForward(object input)
        {
            var value = InternalConvertForward(input);
            if (TypeCheck && !OutputType.IsInstanceOfTypeOrNull(value))
                throw new Exception($"Converted value '{value}' is not a instance of output type '{OutputType}'.");
            return value;
        }

        public object ConvertBackward(object input)
        {
            var value = InternalConvertBackward(input);
            if (TypeCheck && !InputType.IsInstanceOfTypeOrNull(value))
                throw new Exception($"Converted value '{value}' is not a instance of input type '{InputType}'.");
            return value;
        }

        [NotNull] protected virtual ITypeConverter CreateInverted() => Inverse(this);

        protected abstract object InternalConvertForward(object input);

        protected abstract object InternalConvertBackward(object input);

    }

    public abstract class TypeConverter<T1, T2> : ITypeConverter<T1, T2>
    {

        public static readonly Type Type1 = typeof(T1), Type2 = typeof(T2);

        internal sealed class InvertedConverter : TypeConverter<T1, T2>
        {

            public InvertedConverter([NotNull] ITypeConverter<T2, T1> converter) : base(converter ?? throw new ArgumentNullException(nameof(converter))) { }

            public override T2 ConvertForward(T1 input) => Inverse().ConvertBackward(input);

            public override T1 ConvertBackward(T2 input) => Inverse().ConvertForward(input);

            protected override ITypeConverter<T2, T1> CreateInverted() => throw new UnreachableException();

        }

        internal sealed class FuncConverter : TypeConverter<T1, T2>
        {

            [NotNull] private readonly Func<T1, T2> _forward;

            [NotNull] private readonly Func<T2, T1> _backward;

            public FuncConverter([NotNull] Func<T1, T2> forward, [NotNull] Func<T2, T1> backward, [CanBeNull] ITypeConverter<T2, T1> inverted) : base(inverted)
            {
                _forward = forward ?? throw new ArgumentNullException(nameof(forward));
                _backward = backward ?? throw new ArgumentNullException(nameof(backward));
            }

            public override T2 ConvertForward(T1 input) => _forward(input);

            public override T1 ConvertBackward(T2 input) => _backward(input);

            protected override ITypeConverter<T2, T1> CreateInverted() => new TypeConverter<T2, T1>.FuncConverter(_backward, _forward, this);

        }

        internal sealed class CastConverter : TypeConverter<T1, T2>
        {

            public static readonly CastConverter Instance = new CastConverter(null);

            private CastConverter([CanBeNull] ITypeConverter<T2, T1> inverted) : base(inverted) { }

            public override T2 ConvertForward(T1 input) => (T2) (object) input;

            public override T1 ConvertBackward(T2 input) => (T1) (object) input;

            protected override ITypeConverter<T2, T1> CreateInverted() => new TypeConverter<T2, T1>.CastConverter(this);

        }

        internal sealed class ConvertConverter : TypeConverter<T1, T2>
        {

            public static readonly ConvertConverter Instance = new ConvertConverter(null);

            private ConvertConverter([CanBeNull] ITypeConverter<T2, T1> inverted) : base(inverted) { }

            public override T2 ConvertForward(T1 input) => (T2) Convert.ChangeType(input, Type2);

            public override T1 ConvertBackward(T2 input) => (T1) Convert.ChangeType(input, Type1);

            protected override ITypeConverter<T2, T1> CreateInverted() => new TypeConverter<T2, T1>.ConvertConverter(this);

        }

        private ITypeConverter<T2, T1> _inverted;

        protected TypeConverter([CanBeNull] ITypeConverter<T2, T1> inverted) => _inverted = inverted;

        public static ITypeConverter<T1, T2> Of([NotNull] Func<T1, T2> forward, [NotNull] Func<T2, T1> backward) => new FuncConverter(forward, backward, null);

        public static ITypeConverter<T1, T2> DirectCast() => CastConverter.Instance;

        public static ITypeConverter<T1, T2> SystemConvert() => ConvertConverter.Instance;

        public static ITypeConverter<T1, T2> Inverse([NotNull] ITypeConverter<T2, T1> converter)
        {
            if (converter is TypeConverter<T2, T1>.InvertedConverter invert)
                return invert.Inverse();
            return new InvertedConverter(converter);
        }

        public Type InputType => typeof(T1);

        public Type OutputType => typeof(T2);

        public ITypeConverter<T2, T1> Inverse() => _inverted ?? (_inverted = CreateInverted());

        public abstract T2 ConvertForward(T1 input);

        public abstract T1 ConvertBackward(T2 input);

        [NotNull] protected virtual ITypeConverter<T2, T1> CreateInverted() => TypeConverter<T2, T1>.Inverse(this);

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

        [NotNull]
        public static ITypeConverter Validate([NotNull] this ITypeConverter typeConverter, [CanBeNull] Type inputType, [CanBeNull] Type outputType)
        {
            if (IsMatch(typeConverter, inputType, outputType)) return typeConverter;
            typeConverter = typeConverter.Inverse();
            if (IsMatch(typeConverter, inputType, outputType)) return typeConverter;
            throw new ArgumentException($"Invalid type converter for type '{inputType}' -> '{outputType}'");
        }

        public static bool IsExactlyMatch([NotNull] this ITypeConverter typeConverter, [NotNull] Type inputType, [NotNull] Type outputType) => 
            typeConverter.InputType == inputType && typeConverter.OutputType == outputType;

        public static bool IsMatch([NotNull] this ITypeConverter typeConverter, [CanBeNull] Type inputType, [CanBeNull] Type outputType) =>
            (inputType == null || typeConverter.InputType.IsAssignableFrom(inputType)) && (outputType == null || outputType.IsAssignableFrom(typeConverter.OutputType));

        public static ITypeConverter<T1, T2> OfNull2Null<T1, T2>([NotNull] Func<T1, T2> forward, [NotNull] Func<T2, T1> backward) where T1 : class where T2 : class =>
            TypeConverter<T1, T2>.Of(val => val == null ? null : forward(val), val => val == null ? null : backward(val));

        public static ITypeConverter<T1, T2> As<T1, T2>([NotNull] this ITypeConverter typeConverter, bool strict = true)
        {
            if (typeConverter is ITypeConverter<T1, T2> typed) return typed;
            var typeMatch = strict ? typeConverter.IsExactlyMatch(typeof(T1), typeof(T2)) : typeConverter.IsMatch(typeof(T1), typeof(T2));
            if (!typeMatch) throw new ArgumentException("type not match");
            return TypeConverter<T1, T2>.Of(val => (T2)typeConverter.ConvertForward(val), val => (T1)typeConverter.ConvertBackward(val));
        }

    }

}
