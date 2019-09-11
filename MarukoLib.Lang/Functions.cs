using System;

namespace MarukoLib.Lang
{

    public static class Functions
    {

        public static Func<T> Constant<T>(T value) => () => value;

        public static T Identity<T>(T input) => input;

        public static bool NotNull<T>(T input) => input != null;

        public static bool IsNull<T>(T input) => input == null;

        public static TOut Compute<TIn, TOut>(this TIn input, Func<TIn, TOut> func) => func == null ? default : func.Invoke(input);

    }

}
