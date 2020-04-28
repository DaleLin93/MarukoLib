using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public static class FuncUtils
    {

        public static TR InvokeQuietly<TR>(this Func<TR> func, TR defaultValue = default, Action<Exception> exceptionConsumer = null)
            => InvokeQuietly<TR, Exception>(func, defaultValue, exceptionConsumer);

        public static TR InvokeQuietly<TR, TEx>(this Func<TR> func, TR defaultValue = default, Action<TEx> exceptionConsumer = null) where TEx : Exception
        {
            if (func == null) return defaultValue;
            try
            {
                return func();
            }
            catch (TEx e)
            {
                exceptionConsumer?.Invoke(e);
                return defaultValue;
            }
        }

        public static TR InvokeQuietly<TP, TR>(this Func<TP, TR> func, TP param, TR defaultValue = default, Action<Exception> exceptionConsumer = null)
            => InvokeQuietly<TP, TR, Exception>(func, param, defaultValue, exceptionConsumer);

        public static TR InvokeQuietly<TP, TR, TEx>(this Func<TP, TR> func, TP param, TR defaultValue = default, Action<TEx> exceptionConsumer = null) where TEx : Exception
        {
            if (func == null) return defaultValue;
            try
            {
                return func(param);
            }
            catch (TEx e)
            {
                exceptionConsumer?.Invoke(e);
                return defaultValue;
            }
        }

        public static TR InvokeQuietly<TP1, TP2, TR>(this Func<TP1, TP2, TR> func, TP1 param1, TP2 param2, TR defaultValue = default, Action<Exception> exceptionConsumer = null)
            => InvokeQuietly<TP1, TP2, TR, Exception>(func, param1, param2, defaultValue, exceptionConsumer);

        public static TR InvokeQuietly<TP1, TP2, TR, TEx>(this Func<TP1, TP2, TR> func, TP1 param1, TP2 param2, TR defaultValue = default, Action<TEx> exceptionConsumer = null) where TEx : Exception
        {
            if (func == null) return defaultValue;
            try
            {
                return func(param1, param2);
            }
            catch (TEx e)
            {
                exceptionConsumer?.Invoke(e);
                return defaultValue;
            }
        }

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public static Func<TR> ContinueWith<TR>([NotNull] this Func<TR> func, [CanBeNull, ItemCanBeNull] params Func<TR, TR>[] functions) 
            => ArrayUtils.IsNullOrEmpty(functions) ? func : () => functions.NotNull().Aggregate(func(), (current, f) => f(current));

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public static Func<TP, TR> ContinueWith<TP, TR>([NotNull] this Func<TP, TR> func, [CanBeNull, ItemCanBeNull] params Func<TR, TR>[] functions)
            => ArrayUtils.IsNullOrEmpty(functions) ? func : p => functions.NotNull().Aggregate(func(p), (current, f) => f(current));

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public static Func<TP1, TP2, TR> ContinueWith<TP1, TP2, TR>([NotNull] this Func<TP1, TP2, TR> func, [CanBeNull, ItemCanBeNull] params Func<TR, TR>[] functions)
            => ArrayUtils.IsNullOrEmpty(functions) ? func : (p1, p2) => functions.NotNull().Aggregate(func(p1, p2), (current, f) => f(current));

    }

}
