using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public sealed class ArrayQuery 
    {

        [Flags]
        public enum BoundType 
        {
            None = 0, 
            Lower = 1, 
            Upper = 2, 
            All = 3
        }

        public class Bounds
        {

            public static readonly Bounds Unbounded = new Bounds();

            public readonly double Lower, Upper;

            public readonly double Tolerance;

            private Bounds() : this(double.NaN, double.NaN, double.NaN) { }

            public Bounds(double lower, double upper, double tolerance = double.NaN)
            {
                Lower = lower;
                Upper = upper;
                Tolerance = Math.Abs(tolerance);
                Type = (double.IsNaN(Lower) ? BoundType.None : BoundType.Lower) 
                       | (double.IsNaN(Upper) ? BoundType.None : BoundType.Upper);
            }

            public BoundType Type { get; }

            public bool IsProvided(BoundType requirement) => (requirement & Type) == requirement;

            public bool Check(double value)
            {
                bool outOfBound;
                if (double.IsNaN(Tolerance))
                    outOfBound = value < Lower || value > Upper;
                else
                    outOfBound = value < Lower - Tolerance || value > Upper + Tolerance;
                return !outOfBound;
            }

            public override string ToString() => $"[{Lower},{Upper}]{{{Tolerance}}}";

        }

        private interface IValue
        {

            BoundType RequiredBound { get; }

            double Evaluate([NotNull] Bounds bounds);

        }

        private class ConstantValue : IValue
        {

            public ConstantValue(double constant) => Constant = constant;

            public double Constant { get; }

            public BoundType RequiredBound => BoundType.None;

            public double Evaluate(Bounds bounds) => Constant;

        }

        private class DependentValue : IValue
        {

            public enum DependentType
            {
                Start, End
            }

            public static readonly DependentValue StartValue = new DependentValue(DependentType.Start);

            public static readonly DependentValue EndValue = new DependentValue(DependentType.End);

            private DependentValue(DependentType type) => Type = type;

            public DependentType Type { get; }

            public BoundType RequiredBound
            {
                get
                {
                    switch (Type)
                    {
                        case DependentType.Start:
                            return BoundType.Lower;
                        case DependentType.End:
                            return BoundType.Upper;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public double Evaluate(Bounds bounds)
            {
                if (bounds == null) throw new ArgumentException($"bounds is required for dependent value: {Type}.");
                switch (Type)
                {
                    case DependentType.Start:
                        return bounds.Lower;
                    case DependentType.End:
                        return bounds.Upper;
                    default:
                        throw new Exception($"Unsupported dependent type: {Type}."); 
                }
            }
        }

        private interface IExpression
        {

            IReadOnlyCollection<double> Evaluate([NotNull] Bounds bounds, bool strict);

        }

        private class ValueExpression : IExpression
        {

            public ValueExpression([NotNull] IValue value) => Value = value ?? throw new ArgumentNullException(nameof(value));

            public IValue Value { get; }

            public IReadOnlyCollection<double> Evaluate(Bounds bounds, bool strict)
            {
                if (!bounds.IsProvided(Value.RequiredBound))
                {
                    if (strict) throw new ArgumentException("Required bound value not provided.");
                    return EmptyArray<double>.Instance;
                }
                var value = Value.Evaluate(bounds);
                if (strict && !bounds.Check(value)) 
                    throw new ArgumentException($"Value '{value}' exceed bounds {bounds}.");
                return new[] {value};
            } 

        }

        private class RangeExpression : IExpression
        {

            public RangeExpression([NotNull] IValue startValue, [NotNull] IValue endValue, [CanBeNull] IValue stepValue)
            {
                StartValue = startValue ?? throw new ArgumentNullException(nameof(startValue));
                EndValue = endValue ?? throw new ArgumentNullException(nameof(endValue));
                StepValue = stepValue;
            }

            public IValue StartValue { get; }

            public IValue EndValue { get; }

            public IValue StepValue { get; }

            public IReadOnlyCollection<double> Evaluate(Bounds bounds, bool strict)
            {
                if (!bounds.IsProvided(StepValue?.RequiredBound ?? BoundType.None) 
                    || !bounds.IsProvided(StartValue.RequiredBound) || !bounds.IsProvided(EndValue.RequiredBound))
                {
                    if (strict) throw new ArgumentException("Required bound not provided.");
                    return EmptyArray<double>.Instance;
                }
                var startValue = StartValue.Evaluate(bounds);
                var endValue = EndValue.Evaluate(bounds);
                if (strict)
                {
                    if (!bounds.Check(startValue))
                        throw new ArgumentException($"Start value '{startValue}' exceed bounds {bounds}.");
                    if (!bounds.Check(endValue))
                        throw new ArgumentException($"End value '{endValue}' exceed bounds {bounds}.");
                }
                return ArrayUtils.Doubles(startValue, true, endValue, true, StepValue?.Evaluate(bounds) ?? 1);
            }

        }

        public static readonly ITypeConverter<ArrayQuery, string> TypeConverter = TypeConverterExt.OfNull2Null<ArrayQuery, string>(q => q.Query, s => new ArrayQuery(s));

        private readonly ICollection<IExpression> _expressions;

        public ArrayQuery([NotNull] string query)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            _expressions = Parse(query);
        }

        private static ICollection<IExpression> Parse([CanBeNull] string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return EmptyArray<IExpression>.Instance;
            var expressions = new LinkedList<IExpression>();
            foreach (var segment in query.Split(',', ' ').Select(str => str.Trim()).Where(str => !str.IsEmpty()))
                expressions.AddLast(ParseExpression(segment));
            return expressions;
        }

        [SuppressMessage("ReSharper", "ArgumentsStyleOther")]
        private static IExpression ParseExpression([NotNull] string expression)
        {
            int colonIndex;
            if (":".Equals(expression))
                return new RangeExpression(DependentValue.StartValue, DependentValue.EndValue, null);
            if ((colonIndex = expression.IndexOf(':')) != -1)
            {
                var secondColonIndex = expression.IndexOf(':', colonIndex + 1);
                string lPart;
                string mPart;
                string rPart;
                if (secondColonIndex == -1)
                {
                    lPart = expression.Substring(0, colonIndex).Trim();
                    mPart = null;
                    rPart = expression.Substring(colonIndex + 1).Trim();
                }
                else
                {
                    lPart = expression.Substring(0, colonIndex).Trim();
                    mPart = expression.Substring(colonIndex + 1, secondColonIndex - colonIndex - 1).Trim();
                    rPart = expression.Substring(secondColonIndex + 1).Trim();
                }
                return new RangeExpression(
                    startValue: string.IsNullOrWhiteSpace(lPart) ? DependentValue.StartValue : ParseValue(lPart),
                    endValue: string.IsNullOrWhiteSpace(rPart) ? DependentValue.EndValue : ParseValue(rPart), 
                    stepValue: mPart == null ? null : ParseValue(mPart));
            }
            return new ValueExpression(ParseValue(expression));
        }

        private static IValue ParseValue([NotNull] string value)
        {
            switch (value)
            {
                case "start":
                    return DependentValue.StartValue;
                case "end":
                    return DependentValue.EndValue;
                default:
                    return new ConstantValue(double.Parse(value));
            }
        }

        public string Query { get; }

        public IReadOnlyCollection<double> Enumerate(double lowerBound, double upperBound) => Enumerate(new Bounds(lowerBound, upperBound), true);

        public IReadOnlyCollection<double> Enumerate() => Enumerate(Bounds.Unbounded, false);

        public IReadOnlyCollection<double> Enumerate([NotNull] Bounds bounds, bool strict) => 
            _expressions.Aggregate((IReadOnlyCollection<double>)EmptyArray<double>.Instance, (current, expression) =>
            {
                var array = expression.Evaluate(bounds, strict);
                return CollectionUtils.ReadonlyCollection<double>.Unwrap(current)
                    .Concat(CollectionUtils.ReadonlyCollection<double>.Unwrap(array))
                    .AsReadonlyCollection(array.Count + current.Count);
            });

        public IReadOnlyCollection<T> Enumerate<T>(T lowerBound, T upperBound, ITypeConverter<double, T> converter) => Enumerate(new Pair<T>(lowerBound, upperBound), true, converter);

        public IReadOnlyCollection<T> Enumerate<T>(ITypeConverter<double, T> converter) => Enumerate(null, false, converter);

        public IReadOnlyCollection<T> Enumerate<T>([CanBeNull] Pair<T> bounds, bool strict, ITypeConverter<double, T> converter)
        {
            var doubleBounds = bounds == null ? Bounds.Unbounded : new Bounds(converter.ConvertBackward(bounds.Left), converter.ConvertBackward(bounds.Right));
            var collection = Enumerate(doubleBounds, strict);
            return CollectionUtils.ReadonlyCollection<double>.Unwrap(collection)
                .Select(converter.ConvertForward)
                .AsReadonlyCollection(collection.Count);
        }

        public override string ToString() => Query;

    }

    public sealed class ArrayQuery<T> 
    {

        public readonly ArrayQuery BaseQuery;

        public readonly ITypeConverter<double, T> Converter;

        public ArrayQuery(string query, ITypeConverter<double, T> typeConverter) : this(new ArrayQuery(query), typeConverter) { }

        public ArrayQuery(ArrayQuery query, ITypeConverter<double, T> typeConverter)
        {
            BaseQuery = query ?? throw new ArgumentNullException(nameof(query));
            Converter = typeConverter;
        }

        public static ITypeConverter<ArrayQuery<T>, string> CreateTypeConverter(ITypeConverter<double, T> numberConverter) => 
            TypeConverterExt.OfNull2Null<ArrayQuery<T>, string>(q => q.Query, s => new ArrayQuery<T>(s, numberConverter));

        public string Query => BaseQuery.Query;

        public IReadOnlyCollection<T> Enumerate(T lowerBound, T upperBound) => BaseQuery.Enumerate(lowerBound, upperBound, Converter);

        public IReadOnlyCollection<T> Enumerate() => BaseQuery.Enumerate(Converter);

        public override string ToString() => Query;

    }

}