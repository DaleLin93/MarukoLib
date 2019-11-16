using System;

namespace MarukoLib.Lang
{

    public static class Suppliers
    {

        public static Supplier<T> Memoize<T>(this Supplier<T> supplier)
        {
            if (supplier == null) return null;
            var flag = false;
            var value = default(T);
            return () =>
            {
                if (flag) return value;
                flag = true;
                return value = supplier();
            };
        }

        public static Supplier<T> MemoizeWithExpiration<T>(this Supplier<T> supplier, TimeSpan expiration)
        {
            if (supplier == null) return null;
            var flag = false;
            var timestamp = 0L;
            var value = default(T);
            return () =>
            {
                var now = DateTimeUtils.CurrentTimeTicks;
                if (flag && now < timestamp + expiration.Ticks) return value;
                flag = true;
                timestamp = now;
                return value = supplier();
            };
        }

    }

}
