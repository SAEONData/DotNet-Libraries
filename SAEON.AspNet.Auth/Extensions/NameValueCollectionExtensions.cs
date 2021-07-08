#if NET472
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SAEON.AspNet.Auth
{
    public static class NameValueCollectionExtensions
    {
        public static bool ContainsKey(this NameValueCollection source, string key)
        {
            if (source.Get(key) is null)
            {
                return source.AllKeys.Contains(key);
            }

            return true;

        }
    }
}
#endif