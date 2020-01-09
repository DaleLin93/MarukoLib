using System;
using System.Collections.Generic;

namespace MarukoLib.Lang
{

    public static class DictionaryUtils
    {

        public static IDictionary<TK,TV> AsDictionary<TK, TV>(this IReadOnlyDictionary<TK, TV> dict)
        {
            if (dict is IDictionary<TK, TV> d) return d;
            var dictionary = new Dictionary<TK, TV>();
            dictionary.AddAll(dict);
            return dictionary;
        }

        public static TV GetOrDefault<TK, TV>(this IReadOnlyDictionary<TK, TV> cache, TK key, Func<TK, TV> initializer, Action<TV> postAction)
        {
            var val = GetOrDefault(cache, key, initializer);
            postAction?.Invoke(val);
            return val;
        }

        public static TV GetOrDefault<TK, TV>(this IReadOnlyDictionary<TK, TV> cache, TK key, Func<TK, TV> initializer) =>
            cache.ContainsKey(key) ? cache[key] : initializer(key);

        public static TV GetOrDefault<TK, TV>(this IReadOnlyDictionary<TK, TV> cache, TK key, TV defaultValue) =>
            cache.ContainsKey(key) ? cache[key] : defaultValue;

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
