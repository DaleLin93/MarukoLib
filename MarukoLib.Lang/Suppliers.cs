using System;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public static class Suppliers
    {

        public static Supplier<T> Memoize<T>([CanBeNull] this Supplier<T> supplier)
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

        public static Supplier<T> MemoizeWithExpiration<T>([CanBeNull] this Supplier<T> supplier, TimeSpan expiration)
        {
            return MemoizeWithExpiration(supplier, Clocks.SystemTicksClock, expiration);
        }

        public static Supplier<T> MemoizeWithExpiration<T>([CanBeNull] this Supplier<T> supplier, [NotNull] IClock clock, TimeSpan expiration)
        {
            if (supplier == null) return null;
            clock = clock.As(TimeUnit.Tick);
            var expirationTicks = expiration.Ticks;
            var expireAt = clock.Time;
            var value = default(T);
            return () =>
            {
                var now = clock.Time;
                if (now < expireAt) return value;
                value = supplier();
                expireAt = now + expirationTicks;
                return value;
            };
        }

    }

}
