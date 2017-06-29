using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SAEON.Logs
{
    public class ParameterList : Dictionary<string, object> { }

    public static class Logging
    {
        public static bool UseFullName { get; set; } = true;

        public static void Exception(Exception ex, string message = "", params object[] values)
        {
            Log.Error(ex, string.IsNullOrEmpty(message) ? "An exception occured" : message, values);
        }

        public static void Error(string message = "", params object[] values)
        {
            Log.Error(string.IsNullOrEmpty(message) ? "An error occured" : message, values);
        }

        public static void Information(string message, params object[] values)
        {
            Log.Information(message, values);
        }

        private static string GetTypeName(Type type, bool onlyName = false)
        {
            return UseFullName && !onlyName ? type.FullName : type.Name;
        }

        private static string GetParameters(ParameterList parameters)
        {
            string result = string.Empty;
            bool isFirst = true;
            foreach (var kvPair in parameters)
            {
                if (!isFirst) result += ", ";
                isFirst = false;
                result += kvPair.Key + "=";
                if (kvPair.Value == null)
                    result += "Null";
                else if (kvPair.Value is string)
                    result += string.Format("'{0}'", kvPair.Value ?? "");
                //else if (kvPair.Value is Guid)
                //    result += string.Format("{0}", kvPair.Value);
                else
                    result += kvPair.Value.ToString();
            }
            return result;
        }

        public static IDisposable MethodCall(Type type, ParameterList parameters = null, [CallerMemberName] string methodName = "")
        {
            var methodCall = $"{GetTypeName(type)}.{methodName}({GetParameters(parameters)})";
            var result = LogContext.PushProperty("Method", methodCall);
            Log.Verbose(methodCall);
            return result;
        }

        public static IDisposable MethodCall<TEntity>(Type type, ParameterList parameters = null, [CallerMemberName] string methodName = "")
        {
            var methodCall = $"{GetTypeName(type)}.{methodName}<{GetTypeName(typeof(TEntity), true)}>({GetParameters(parameters)})";
            var result = LogContext.PushProperty("Method", methodCall);
            Log.Verbose(methodCall);
            return result;
        }

        public static IDisposable MethodCall<TEntity, TRelatedEntity>(Type type, ParameterList parameters = null, [CallerMemberName] string methodName = "")
        {
            var methodCall = $"{GetTypeName(type)}.{methodName}<{GetTypeName(typeof(TEntity), true)},{GetTypeName(typeof(TRelatedEntity), true)}>({GetParameters(parameters)})";
            var result = LogContext.PushProperty("Method", methodCall);
            Log.Verbose(methodCall);
            return result;
        }

        public static void Verbose(string message, params object[] values)
        {
            Log.Verbose(message, values);
        }

    }
}
