#if NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP1_1
using Microsoft.Extensions.Configuration;
#endif
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SAEON.Logs
{
    public class ParameterList : Dictionary<string, object> { }

    public static class Logging  
    { 
        public static bool UseFullName { get; set; } = true;

#if NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP1_1
        public static LoggerConfiguration CreateConfiguration(string fileName, IConfiguration config)
#else
        public static LoggerConfiguration CreateConfiguration(string fileName)
#endif
        {
            return new LoggerConfiguration()
#if NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP1_1
                .ReadFrom.Configuration(config)
#else 
                .ReadFrom.AppSettings()
#endif
                .Enrich.FromLogContext()
                .WriteTo.RollingFile(fileName)
                .WriteTo.Seq("http://localhost:5341/");
        }
         
        public static void Create(this LoggerConfiguration config)
        {
            Log.Logger = config.CreateLogger();
        }

        public static void Exception(Exception ex, string message = "", params object[] values)
        {
            Log.Error(ex, string.IsNullOrEmpty(message) ? "An exception occurred" : message, values);
        }

        public static void Error(string message = "", params object[] values)
        {
            Log.Error(string.IsNullOrEmpty(message) ? "An error occurred" : message, values);
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
            if (parameters != null)
            {
                bool isFirst = true;
                foreach (var kvPair in parameters)
                {
                    if (!isFirst) result += ", ";
                    isFirst = false;
                    result += kvPair.Key + "=";
                    if (kvPair.Value == null)
                        result += "Null";
                    else if (kvPair.Value is string)
                        result += string.Format("'{0}'", kvPair.Value);
                    else if (kvPair.Value is Guid)
                        result += string.Format("{0}", kvPair.Value);
                    else
                        result += kvPair.Value.ToString();
                }
            }
            return result;
        }

        public static string MethodSignature(Type type, string methodName, ParameterList parameters = null)
        {
            return $"{GetTypeName(type)}.{methodName}({GetParameters(parameters)})";
        }
         
        public static string MethodSignature(Type type, string methodName, string entityTypeName, ParameterList parameters = null)
        {
            return $"{GetTypeName(type)}.{methodName}<{entityTypeName}>({GetParameters(parameters)})";
        }

        public static string MethodSignature(Type type, string methodName, string entityTypeName, string relatedEntityTypeName, ParameterList parameters = null)
        {
            return $"{GetTypeName(type)}.{methodName}<{entityTypeName},{relatedEntityTypeName}>({GetParameters(parameters)})";
        }

        public static IDisposable MethodCall(Type type, ParameterList parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodSignature(type, methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity>(Type type, ParameterList parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodSignature(type, methodName, GetTypeName(typeof(TEntity), true), parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity, TRelatedEntity>(Type type, ParameterList parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodSignature(type, methodName, GetTypeName(typeof(TEntity), true), GetTypeName(typeof(TRelatedEntity), true), parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static void Verbose(string message, params object[] values)
        {
            Log.Verbose(message, values);
        }

    }
}
