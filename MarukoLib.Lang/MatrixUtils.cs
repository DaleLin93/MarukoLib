using System;
using System.Collections.Generic;

namespace MarukoLib.Lang
{

    public static class EmptyMatrix<T>
    {

        public static readonly T[,] Instance = new T[0, 0];

    }

    public static class MatrixUtils
    {

        public enum MatrixDimension
        {
            Row = 0, Col = 1
        }

        public static int GetRowCount<T>(this T[,] mat) => mat.GetLength(0);

        public static int GetColCount<T>(this T[,] mat) => mat.GetLength(1);

        public static T[,] Concat<T>(this T[,] mat, T[,] another, MatrixDimension dim)
        {
            if (dim != MatrixDimension.Row && dim != MatrixDimension.Col)
                throw new ArgumentException($"Illegal dim: {dim}");
            if (mat == null || mat.Length == 0)
                return another ?? EmptyMatrix<T>.Instance;
            if (another == null || another.Length == 0)
                return mat;
            var concatDim = dim == MatrixDimension.Row ? MatrixDimension.Col : MatrixDimension.Row;
            var keptDim = dim;
            var keptDimLength = mat.GetLength((int)keptDim);
            if (keptDimLength != another.GetLength((int)keptDim))
                throw new ArgumentException("Dimensions of matrices being concatenated are not consistent, "
                                            + $"m1: {mat.GetLength(0)}x{mat.GetLength(1)}, m2: {another.GetLength(0)}x{another.GetLength(1)}, "
                                            + $"dim: {dim}");
            var m1DimLength = mat.GetLength((int)concatDim);
            var concatDimLength = m1DimLength + another.GetLength((int)concatDim);
            var dimLengths = new int[2];
            dimLengths[(int)concatDim] = concatDimLength;
            dimLengths[(int)keptDim] = keptDimLength;
            var row = dimLengths[0];
            var col = dimLengths[1];
            var matrix = new T[row, col];
            Func<int, int, T> getFunc;
            if (concatDim == MatrixDimension.Row)
                getFunc = (r, c) => r >= m1DimLength ? another[r - m1DimLength, c] : mat[r, c];
            else
                getFunc = (r, c) => c >= m1DimLength ? another[r, c - m1DimLength] : mat[r, c];
            for (var r = 0; r < dimLengths[0]; r++)
                for (var c = 0; c < dimLengths[1]; c++)
                    matrix[r, c] = getFunc(r, c);
            return matrix;
        }

        public static T[] GetRow<T>(this T[,] mat, int row)
        {
            var result = new T[mat.GetLength(0)];
            GetRow(mat, row, result, 0);
            return result;
        }

        public static void GetRow<T>(this T[,] src, int row, T[] dst, int startIndex = 0)
        {
            var width = src.GetLength(0);
            var height = src.GetLength(1);

            // Ensures the row requested is within the range of the 2-d array
            if (row >= height)
                throw new IndexOutOfRangeException("row index out of range");
            if (dst.Length - startIndex < width)
                throw new ArgumentException("dst array length is not enough");

            for (var i = 0; i < width; i++)
                dst[startIndex + i] = src[i, row];
        }

        public static IReadOnlyCollection<T[]> GetRows<T>(this T[,] mat)
        {
            var cols = mat.GetColCount();
            return EnumerableUtils.Enumerate(cols, col => GetRow(mat, col)).AsReadonlyCollection(cols);
        }

    }
}
