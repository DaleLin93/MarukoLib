using System;
using System.Collections.Generic;

namespace MarukoLib.Lang
{

    public static class DictionaryUtils
    {

        public static TV GetOrCreate<TK, TV>(this IDictionary<TK, TV> cache, TK key, Func<TK, TV> initializer, Action<TV> postAction)
        {
            var val = GetOrCreate(cache, key, initializer);
            postAction?.Invoke(val);
            return val;
        }

        public static TV GetOrCreate<TK, TV>(this IDictionary<TK, TV> cache, TK key, Func<TK, TV> initializer) =>
            cache.ContainsKey(key) ? cache[key] : cache[key] = initializer(key);

    }
}
