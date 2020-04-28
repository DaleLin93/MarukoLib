using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public static class DictionaryUtils
    {

        public static IDictionary<TK, TV> AsWritable<TK, TV>([NotNull] this IReadOnlyDictionary<TK, TV> dict) 
            => dict is IDictionary<TK, TV> d ? d : Copy(dict);

        public static Dictionary<TK, TV> Copy<TK, TV>([NotNull] this IReadOnlyDictionary<TK, TV> dict)
        {
            var dictionary = new Dictionary<TK, TV>();
            dictionary.AddAll(dict);
            return dictionary;
        }

        public static Dictionary<TK, TV> Copy<TK, TV>([NotNull] this IDictionary<TK, TV> dict)
        {
            var dictionary = new Dictionary<TK, TV>();
            dictionary.AddAll(dict);
            return dictionary;
        }

        public static Dictionary<TK, TV> OfKeys<TK, TV>([NotNull] this IDictionary<TK, TV> dict, [CanBeNull] IEnumerable<TK> keys)
        {
            var dictionary = new Dictionary<TK, TV>();
            if (keys == null)
                dictionary.PutAll(dict);
            else
                foreach (var key in keys)
                    if (dict.TryGetValue(key, out var val))
                        dictionary[key] = val;
            return dictionary;
        }

        public static Dictionary<TK, TV> OfKeys<TK, TV>([NotNull] this IReadOnlyDictionary<TK, TV> dict, [CanBeNull] IEnumerable<TK> keys)
        {
            var dictionary = new Dictionary<TK, TV>();
            if (keys == null)
                dictionary.PutAll(dict);
            else
                foreach (var key in keys)
                    if (dict.TryGetValue(key, out var val))
                        dictionary[key] = val;
            return dictionary;
        }

        public static TV GetOrDefault<TK, TV>([NotNull] this IReadOnlyDictionary<TK, TV> cache, TK key, 
            [NotNull] Func<TK, TV> initializer, [CanBeNull] Action<TV> postAction)
        {
            var val = GetOrDefault(cache, key, initializer);
            postAction?.Invoke(val);
            return val;
        }

        public static TV GetOrDefault<TK, TV>([NotNull] this IReadOnlyDictionary<TK, TV> cache, TK key, [NotNull] Func<TK, TV> initializer) =>
            cache.ContainsKey(key) ? cache[key] : initializer(key);

        public static TV GetOrDefault<TK, TV>([NotNull] this IReadOnlyDictionary<TK, TV> cache, TK key, TV defaultValue) =>
            cache.ContainsKey(key) ? cache[key] : defaultValue;

        public static TV GetOrCreate<TK, TV>([NotNull] this IDictionary<TK, TV> cache, TK key, 
            [NotNull] Func<TK, TV> initializer, [CanBeNull] Action<TV> postAction)
        {
            var val = GetOrCreate(cache, key, initializer);
            postAction?.Invoke(val);
            return val;
        }

        public static TV GetOrCreate<TK, TV>([NotNull] this IDictionary<TK, TV> cache, TK key, [NotNull]  Func<TK, TV> initializer) =>
            cache.ContainsKey(key) ? cache[key] : cache[key] = initializer(key);

        public static void PutAll<TK, TV>([NotNull] this IDictionary<TK, TV> dictionary, [CanBeNull] IDictionary<TK, TV> dict2)
        {
            if (dict2 == null) return;
            foreach (var pair in dict2) 
                dictionary[pair.Key] = pair.Value;
        }

        public static void PutAll<TK, TV>([NotNull] this IDictionary<TK, TV> dictionary, [CanBeNull] IReadOnlyDictionary<TK, TV> dict2)
        {
            if (dict2 == null) return;
            foreach (var pair in dict2)
                dictionary[pair.Key] = pair.Value;
        }

    }
}
