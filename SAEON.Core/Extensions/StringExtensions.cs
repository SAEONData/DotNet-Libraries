using System;
using System.Collections.Generic;

namespace SAEON.Core
{
    public static class StringExtensions
    {
        public static string AddTrailing(this string source, string trailing)
        {
            return source.EndsWith(trailing) ? source : source + trailing;
        }

        public static string AddTrailingForwardSlash(this string source)
        {
            return source.AddTrailing("/");
        }

        public static string AddTrailingBackSlash(this string source)
        {
            return source.AddTrailing("\\");
        }

        public static string DoubleQuoted(this string source)
        {
            return source.Quoted('"');
        }

        public static bool IsTrue(this string source)
        {
            if (bool.TryParse(source, out bool result))
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

        public static string RemoveHttp(this string source)
        {
            return source.Replace("https://", string.Empty).Replace("http://", string.Empty);
        }

        public static string Replace(this string source, Dictionary<string, string> dictionary)
        {
            if (dictionary is null) return source;
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

        public static string TrimEnd(this string source, string search, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(search)) return source;
            while (source.EndsWith(search, comparisonType))
                source = source.Remove(source.Length - search.Length);
            return source.TrimEnd();
        }

        public static string TrimStart(this string source, string search, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(search)) return source;
            while (source.StartsWith(search, comparisonType))
                source = source.Remove(0, search.Length);
            return source.TrimStart();
        }
    }
}
