using System;
using System.Collections.Generic;

namespace SAEON.Core
{
    public static class StringExtensions 
    {

        public static string DoubleQuoted(this string source)
        { 
            return source.Quoted('"');
        }

        public static string Quoted(this string source, char quote)
        {
            return
                quote + source.Replace(Convert.ToString(quote), Convert.ToString(quote) + Convert.ToString(quote)) + quote;
        }


        public static string Replace(this string source, Dictionary<string, string> dictionary)
        {
            string result = source;
            foreach (var kv in dictionary) result = result.Replace(kv.Key, kv.Value);
            return result;
        }

        public static string SingleQuoted(this string source)
        {
            return source.Quoted('\'');
        }

        public static string TrimEnd(this string source, string suffix)
        {
            return !source.EndsWith(suffix) ? suffix : source.Remove(source.Length - suffix.Length);
        }
    }
}
