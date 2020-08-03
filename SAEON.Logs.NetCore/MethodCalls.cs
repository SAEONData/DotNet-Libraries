using System;
using System.Collections.Generic;

namespace SAEON.Logs
{
#pragma warning disable CA2237 // Mark ISerializable types with serializable
    public class MethodCallParameters : Dictionary<string, object> { }
#pragma warning restore CA2237 // Mark ISerializable types with serializable

    public static class MethodCalls
    {
        public static bool UseFullName { get; set; } = true;

        private static string GetTypeName(Type type, bool onlyName = false)
        {
            //return UseFullName && !onlyName ? type.FullName : type.Name;
            var typeName = type.IsGenericType ? type.Name.Split('`')[0] : type.Name;
            return UseFullName && !onlyName ? $"{type.Namespace}.{typeName}".TrimStart('.') : typeName;
        }

        private static string GetParameters(MethodCallParameters parameters)
        {
            string result = string.Empty;
            if (parameters != null)
            {
                bool isFirst = true;
                foreach (var kvPair in parameters)
                {
                    if (!isFirst)
                    {
                        result += ", ";
                    }

                    isFirst = false;
                    result += kvPair.Key + "=";
                    if (kvPair.Value == null)
                    {
                        result += "Null";
                    }
                    else
                    {
                        result += kvPair.Value switch
                        {
                            string str => $"'{kvPair.Value}'",
                            List<string> list => $"[{string.Join(",", list)}]",
                            string[] strings => $"[{string.Join(",", strings)}]",
                            _ => $"{kvPair.Value}",
                        };
                    }
                }
            }
            return result;
        }

        public static string MethodSignature(Type type, string methodName, MethodCallParameters parameters = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return $"{GetTypeName(type)}.{methodName}({GetParameters(parameters)})".Replace("..", ".");
        }

        public static string MethodSignature(Type type, Type entityType, string methodName, MethodCallParameters parameters = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            return $"{GetTypeName(type)}<{GetTypeName(entityType)}>.{methodName}({GetParameters(parameters)})".Replace("..", ".");
        }

        public static string MethodSignature(Type type, Type entityType, Type relatedEntityType, string methodName, MethodCallParameters parameters = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (relatedEntityType == null) throw new ArgumentNullException(nameof(relatedEntityType));
            return $"{GetTypeName(type)}<{GetTypeName(entityType)},{GetTypeName(relatedEntityType)}>.{methodName}({GetParameters(parameters)})".Replace("..", ".");
        }

    }

}
