using System.Collections.Generic;

namespace SAEON.Core
{
    public static class StringExtensions  
    {
        public static string Replace(this string source, Dictionary<string, string> dictionary)
        {
            string result = source;
            foreach (var kv in dictionary) result = result.Replace(kv.Key, kv.Value);
            return result;
        }
    } 
} 
