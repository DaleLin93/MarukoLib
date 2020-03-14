using System;
using System.Collections.Generic;
using System.Net;
using JetBrains.Annotations;

namespace MarukoLib.Net
{

    public static class CookieUtils
    {

        /// <summary>
        /// Try to get the first value for specific cookie key.
        /// </summary>
        /// <returns></returns>
        public static bool TryGetFirstCookie([NotNull] this CookieContainer container, [NotNull] Uri uri, [NotNull] string key, out string value)
        {
            foreach (Cookie cookie in container.GetCookies(uri))
                if (key.Equals(cookie.Name))
                {
                    value = cookie.Value;
                    return true;
                }
            value = null;
            return false;
        }

        /// <summary>
        /// Try to get the first value for specific cookie key.
        /// </summary>
        /// <returns></returns>
        public static ICollection<string> GetCookies([NotNull] this CookieContainer container, [NotNull] Uri uri, [NotNull] string key)
        {
            var list = new LinkedList<string>();
            foreach (Cookie cookie in container.GetCookies(uri))
                if (key.Equals(cookie.Name))
                    list.AddLast(cookie.Value);
            return list;
        }

    }

}
