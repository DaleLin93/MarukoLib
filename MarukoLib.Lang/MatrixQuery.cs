using System;
using System.Linq;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public sealed class MatrixQuery
    {

        private interface IExpression
        {

            double[,] Evaluate([NotNull] ArrayQuery.Bounds bounds, bool strict);

        }

        private class VectorExpression : IExpression
        {

            public VectorExpression([NotNull] ArrayQuery array, bool row = true)
            {
                Vector = array ?? throw new ArgumentNullException(nameof(array));
                Row = row;
            }

            public ArrayQuery Vector { get; }

            public bool Row { get; }

            public double[,] Evaluate(ArrayQuery.Bounds bounds, bool strict)
            {
                var vector = Vector.Enumerate(bounds, strict);
                double[,] matrix;
                Action<int, double> setFunc;
                if (Row)
                {
                    matrix = new double[1, vector.Count];
                    setFunc = (idx, val) => matrix[0, idx] = val;
                }
                else
                {
                    matrix = new double[vector.Count, 1];
                    setFunc = (idx, val) => matrix[idx, 0] = val;
                }
                var i = 0;
                foreach (var val in vector)
                    setFunc(i++, val);
                return matrix;
            }

        }

        private class MergeExpression : IExpression
        {

            public MergeExpression([NotNull] IExpression[] expressions, bool row = true)
            {
                Expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
                Row = row;
            }

            public IExpression[] Expressions { get; }

            public bool Row { get; }

            public double[,] Evaluate(ArrayQuery.Bounds bounds, bool strict) =>
                Expressions.Aggregate(EmptyMatrix<double>.Instance, (current, expression) => 
                    current.Concat(expression.Evaluate(bounds, strict), Row ? MatrixUtils.MatrixDimension.Row : MatrixUtils.MatrixDimension.Col));

        }

        public static readonly ITypeConverter<MatrixQuery, string> TypeConverter = TypeConverterExt.OfNull2Null<MatrixQuery, string>(q => q.Query, s => new MatrixQuery(s));

        private readonly IExpression _expression;

        public MatrixQuery([NotNull] string query)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            _expression = Parse(query);
        }

        private static IExpression Parse([CanBeNull] string query) => string.IsNullOrWhiteSpace(query) ? null : Parse(query, false);

        private static IExpression Parse([NotNull] string expression, bool row)
        {
            var subExpressions = (row ? expression.Split(',', ' ') : expression.Split(';'))
                .Select(str => str.Trim())
                .Where(str => !str.IsEmpty())
                .Select(str => row ? ParseVector(str) : Parse(str, true))
                .ToArray();
            if (subExpressions.IsEmpty()) return null;
            return subExpressions.Length == 1 ? subExpressions[0] : new MergeExpression(subExpressions, row);
        }

        private static IExpression ParseVector([NotNull] string expression)
        {
            var rowVec = true;
            while (expression.EndsWith("'"))
            {
                rowVec = !rowVec;
                expression = expression.Substring(0, expression.Length - 1);
            }
            return new VectorExpression(new ArrayQuery(expression), rowVec);
        }

        public string Query { get; }

        public double[,] GetMatrix(double lowerBound, double upperBound) => GetMatrix(new ArrayQuery.Bounds(lowerBound, upperBound), true);

        public double[,] GetMatrix() => GetMatrix(ArrayQuery.Bounds.Unbounded, false);

        public double[,] GetMatrix([NotNull] ArrayQuery.Bounds bounds, bool strict) => _expression?.Evaluate(bounds, strict) ?? EmptyMatrix<double>.Instance;

        public T[,] GetMatrix<T>(T lowerBound, T upperBound, ITypeConverter<double, T> converter) => GetMatrix(new Pair<T>(lowerBound, upperBound), true, converter);

        public T[,] GetMatrix<T>(ITypeConverter<double, T> converter) => GetMatrix(null, false, converter);

        public T[,] GetMatrix<T>([CanBeNull] Pair<T> bounds, bool strict, ITypeConverter<double, T> converter)
        {
            var doubleBounds = bounds == null ? ArrayQuery.Bounds.Unbounded 
                : new ArrayQuery.Bounds(converter.ConvertBackward(bounds.Left), converter.ConvertBackward(bounds.Right));
            var matrix = GetMatrix(doubleBounds, strict);
            var row = matrix.GetLength(0);
            var col = matrix.GetLength(1);
            var result = new T[row, col];
            for (var r = 0; r < row; r++)
            for (var c = 0; c < col; c++)
                result[r, c] = converter.ConvertForward(matrix[r, c]);                    
            return result;
        }

        public override string ToString() => Query;

    }

    public sealed class MatrixQuery<T> 
    {

        public readonly MatrixQuery BaseQuery;

        public readonly ITypeConverter<double, T> Converter;

        public MatrixQuery(string query, ITypeConverter<double, T> typeConverter)
        {
            BaseQuery = new MatrixQuery(query ?? throw new ArgumentNullException(nameof(query)));
            Converter = typeConverter;
        }

        public static ITypeConverter<MatrixQuery<T>, string> CreateTypeConverter(ITypeConverter<double, T> numberConverter) =>
            TypeConverterExt.OfNull2Null<MatrixQuery<T>, string>(q => q.Query, s => new MatrixQuery<T>(s, numberConverter));

        public string Query => BaseQuery.Query;

        public T[,] GetMatrix(T lowerbound, T upperbound) => BaseQuery.GetMatrix(lowerbound, upperbound, Converter);

        public T[,] GetMatrix() => BaseQuery.GetMatrix(Converter);

        public override string ToString() => Query;

    }

}