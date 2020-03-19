using System.Net;
using JetBrains.Annotations;

namespace MarukoLib.Net
{

    public static class RequestUtils
    {

        public static string GetFirstQueryValue([NotNull] this HttpListenerRequest request, [NotNull] string key)
        {
            var values = request.QueryString.GetValues(key);
            if (values == null || values.Length == 0) return null;
            return values[0];
        }

    }

}
