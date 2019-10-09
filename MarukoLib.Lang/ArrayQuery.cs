using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public sealed class ArrayQuery 
    {

        private interface IValue
        {

            bool IsBoundsRequired { get; }

            double Evaluate([CanBeNull] Pair<double> bounds);

        }

        private class ConstantValue : IValue
        {

            public ConstantValue(double constant) => Constant = constant;

            public double Constant { get; }

            public bool IsBoundsRequired => false;

            public double Evaluate(Pair<double> bounds) => Constant;

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

            public bool IsBoundsRequired => true;

            public double Evaluate(Pair<double> bounds)
            {
                if (bounds == null) throw new ArgumentException($"bounds is required for dependent value: {Type}");
                switch (Type)
                {
                    case DependentType.Start:
                        return bounds.Left;
                    case DependentType.End:
                        return bounds.Right;
                    default:
                        throw new Exception($"Unsupported dependent type: {Type}"); 
                }
            }
        }

        private interface IExpression
        {

            IReadOnlyCollection<double> Evaluate([CanBeNull] Pair<double> bounds, bool strict);

        }

        private class ValueExpression : IExpression
        {

            public ValueExpression([NotNull] IValue value) => Value = value ?? throw new ArgumentNullException(nameof(value));

            public IValue Value { get; }

            public IReadOnlyCollection<double> Evaluate(Pair<double> bounds, bool strict)
            {
                if (bounds == null && Value.IsBoundsRequired)
                {
                    if (strict) throw new ArgumentException("bounds not provided");
                    return EmptyArray<double>.Instance;
                }
                return new[] {Value.Evaluate(bounds)};
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

            public IReadOnlyCollection<double> Evaluate(Pair<double> bounds, bool strict)
            {
                if (bounds == null && (StartValue.IsBoundsRequired || EndValue.IsBoundsRequired || (StepValue?.IsBoundsRequired ?? false)))
                {
                    if (strict) throw new ArgumentException("bounds not provided");
                    return EmptyArray<double>.Instance;
                }
                return ArrayUtils.Doubles(StartValue.Evaluate(bounds), true, EndValue.Evaluate(bounds), true, StepValue?.Evaluate(bounds) ?? 1);
            }

        }

        public static readonly ITypeConverter<ArrayQuery, string> TypeConverter = TypeConverterExt.OfNull2Null<ArrayQuery, string>(q => q.Query, s => new ArrayQuery(s));

        private readonly ICollection<IExpression> _expressions;

        public ArrayQuery(string query)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            _expressions = Parse(query);
        }

        private static ICollection<IExpression> Parse(string query)
        {
            if (query.IsBlank()) return EmptyArray<IExpression>.Instance;
            var expressions = new LinkedList<IExpression>();
            foreach (var segment in query.Split(',', ' ').Select(str => str.Trim()).Where(str => !str.IsEmpty()))
                expressions.AddLast(ParseExpression(segment));
            return expressions;
        }

        private static IExpression ParseExpression(string expression)
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
                    lPart.IsBlank() ? DependentValue.StartValue : ParseValue(lPart),
                    rPart.IsBlank() ? DependentValue.EndValue : ParseValue(rPart),
                    mPart == null ? null : ParseValue(mPart));
            }
            return new ValueExpression(ParseValue(expression));
        }

        private static IValue ParseValue(string value)
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

        public IReadOnlyCollection<double> Enumerate(double lowerbound, double upperbound) => Enumerate(new Pair<double>(lowerbound, upperbound), true);

        public IReadOnlyCollection<double> Enumerate() => Enumerate(null, false);

        public IReadOnlyCollection<double> Enumerate([CanBeNull] Pair<double> bounds, bool strict) => 
            _expressions.Aggregate((IReadOnlyCollection<double>)EmptyArray<double>.Instance, (current, expression) =>
            {
                var array = expression.Evaluate(bounds, strict);
                return CollectionUtils.ReadonlyCollection<double>.Unwrap(current)
                    .Concat(CollectionUtils.ReadonlyCollection<double>.Unwrap(array))
                    .AsReadonlyCollection(array.Count + current.Count);
            });

        public IReadOnlyCollection<T> Enumerate<T>(T lowerbound, T upperbound, ITypeConverter<double, T> converter) => Enumerate(new Pair<T>(lowerbound, upperbound), true, converter);

        public IReadOnlyCollection<T> Enumerate<T>(ITypeConverter<double, T> converter) => Enumerate(null, false, converter);

        public IReadOnlyCollection<T> Enumerate<T>([CanBeNull] Pair<T> bounds, bool strict, ITypeConverter<double, T> converter)
        {
            var doubleBounds = bounds == null ? null : new Pair<double>(converter.ConvertBackward(bounds.Left), converter.ConvertBackward(bounds.Right));
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

        public ArrayQuery(string query, ITypeConverter<double, T> typeConverter)
        {
            BaseQuery = new ArrayQuery(query ?? throw new ArgumentNullException(nameof(query)));
            Converter = typeConverter;
        }

        public static ITypeConverter<ArrayQuery<T>, string> CreateTypeConverter(ITypeConverter<double, T> numberConverter) => 
            TypeConverterExt.OfNull2Null<ArrayQuery<T>, string>(q => q.Query, s => new ArrayQuery<T>(s, numberConverter));

        public string Query => BaseQuery.Query;

        public IReadOnlyCollection<T> Enumerate(T lowerbound, T upperbound) => BaseQuery.Enumerate(lowerbound, upperbound, Converter);

        public IReadOnlyCollection<T> Enumerate() => BaseQuery.Enumerate(Converter);

        public override string ToString() => Query;

    }

}