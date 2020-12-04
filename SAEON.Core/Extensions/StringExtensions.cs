using System;
using System.Collections.Generic;

namespace SAEON.Core
{
    public static class StringExtensions
    {
        public static string AddTrailingForwardSlash(this string value)
        {
            if (value.EndsWith("/", StringComparison.InvariantCulture)) return value;
            return value + "/";
        }
        public static string AddTrailingBackSlash(this string value)
        {
            if (value.EndsWith("\\", StringComparison.InvariantCulture)) return value;
            return value + "\\";
        }

        public static string DoubleQuoted(this string value)
        {
            return value.Quoted('"');
        }

        public static bool IsTrue(this string value)
        {
            if (bool.TryParse(value, out bool result))
                return result;
            else
                return false;
        }

        public static string Quoted(this string value, char quote)
        {
            return
#if NET472
                quote + value.Replace($"{quote}", $"{quote}{quote}") + quote;
#else
                quote + value.Replace($"{quote}", $"{quote}{quote}", StringComparison.CurrentCultureIgnoreCase) + quote;
#endif
        }

        public static string Replace(this string value, Dictionary<string, string> dictionary)
        {
            if (dictionary == null) return value;
            string result = value;
            foreach (var kv in dictionary)
            {
#if NET472
                result = result.Replace(kv.Key, kv.Value);
#else
                result = result.Replace(kv.Key, kv.Value, StringComparison.CurrentCultureIgnoreCase);
#endif
            }

            return result;
        }

        public static string SingleQuoted(this string value)
        {
            return value.Quoted('\'');
        }

        public static string TrimStart(this string value, string prefix)
        {
            if (prefix == null) return value;
            return !value.StartsWith(prefix, StringComparison.InvariantCulture) ? value : value.Remove(0, prefix.Length);
        }

        public static string TrimEnd(this string value, string suffix)
        {
            if (suffix == null) return value;
            return !value.EndsWith(suffix, StringComparison.InvariantCulture) ? value : value.Remove(value.Length - suffix.Length);
        }
    }
}
