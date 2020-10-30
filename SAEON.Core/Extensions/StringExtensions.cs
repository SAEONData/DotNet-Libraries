using System;
using System.Collections.Generic;

namespace SAEON.Core
{
    public static class StringExtensions
    {
        public static string AddTrailingForwardSlash(this string aString)
        {
            if (aString.EndsWith("/", StringComparison.InvariantCulture)) return aString;
            return aString + "/";
        }
        public static string AddTrailingBackSlash(this string aString)
        {
            if (aString.EndsWith("\\", StringComparison.InvariantCulture)) return aString;
            return aString + "\\";
        }

        public static string DoubleQuoted(this string source)
        {
            return source.Quoted('"');
        }

        public static bool IsTrue(this string value)
        {
            if (bool.TryParse(value, out bool result))
                return result;
            else
                return false;
        }

        public static string Quoted(this string source, char quote)
        {
            return
#if NET472
                quote + source.Replace($"{quote}", $"{quote}{quote}") + quote;
#else
                quote + source.Replace($"{quote}", $"{quote}{quote}", StringComparison.CurrentCultureIgnoreCase) + quote;
#endif
        }

        public static string Replace(this string source, Dictionary<string, string> dictionary)
        {
            if (dictionary == null) return source;
            string result = source;
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

        public static string SingleQuoted(this string source)
        {
            return source.Quoted('\'');
        }

        public static string TrimStart(this string source, string prefix)
        {
            if (prefix == null) return source;
            return !source.StartsWith(prefix, StringComparison.InvariantCulture) ? source : source.Remove(0, prefix.Length);
        }

        public static string TrimEnd(this string source, string suffix)
        {
            if (suffix == null) return source;
            return !source.EndsWith(suffix, StringComparison.InvariantCulture) ? source : source.Remove(source.Length - suffix.Length);
        }
    }
}
