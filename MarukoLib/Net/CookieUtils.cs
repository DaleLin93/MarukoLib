using System;
using System.Net;

namespace MarukoLib.Net
{

    public static class CookieUtils
    {

        /// <summary>
        /// 获取指定指定cookie的值
        /// </summary>
        /// <returns></returns>
        public static bool TryGetCookieValue(CookieContainer container, Uri uri, string key, out string value)
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

    }

}
