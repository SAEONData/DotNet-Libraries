using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SAEON.Logs
{
    public class LogParameters : Dictionary<string, object> { }

    [Obsolete("SAEON.Logging is obsolete. Use SAEON.Logs.NetCore or SAEON.Logs.NetFramework")]
    public static class Logging
    {
        public static bool UseFullName { get; set; } = true;

        public static LoggerConfiguration CreateConfiguration(string fileName, IConfiguration config)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext()
                .WriteTo.File(fileName, rollOnFileSizeLimit: true, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1), rollingInterval: RollingInterval.Day, retainedFileCountLimit: null)
                //.WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341/");
        }

        public static void Create(this LoggerConfiguration config)
        {
            Log.Logger = config.CreateLogger();
        }

        public static void ShutDown()
        {
            Information("Shutting logging down");
            Log.CloseAndFlush();
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
            //return UseFullName && !onlyName ? type.FullName : type.Name;
            var typeName = type.IsGenericType ? type.Name.Split('`')[0] : type.Name;
            return UseFullName && !onlyName ? $"{type.Namespace}.{typeName}".TrimStart('.') : typeName;
        }

        private static string GetParameters(LogParameters parameters)
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
                    else if (kvPair.Value is string)
                    {
                        result += string.Format("'{0}'", kvPair.Value);
                    }
                    else if (kvPair.Value is Guid)
                    {
                        result += string.Format("{0}", kvPair.Value);
                    }
                    else
                    {
                        result += kvPair.Value.ToString();
                    }
                }
            }
            return result;
        }

        public static string MethodSignature(Type type, string methodName, LogParameters parameters = null)
        {
            return $"{GetTypeName(type)}.{methodName}({GetParameters(parameters)})".Replace("..", ".");
        }

        public static string MethodSignature(Type type, Type entityType, string methodName, LogParameters parameters = null)
        {
            return $"{GetTypeName(type)}<{GetTypeName(entityType)}>.{methodName}({GetParameters(parameters)})".Replace("..", ".");
        }

        public static string MethodSignature(Type type, Type entityType, Type relatedEntityType, string methodName, LogParameters parameters = null)
        {
            return $"{GetTypeName(type)}<{GetTypeName(entityType)},{GetTypeName(relatedEntityType)}>.{methodName}({GetParameters(parameters)})".Replace("..", ".");
        }

        public static IDisposable MethodCall(Type type, LogParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodSignature(type, methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity>(Type type, LogParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodSignature(type, typeof(TEntity), methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity, TRelatedEntity>(Type type, LogParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodSignature(type, typeof(TEntity), typeof(TRelatedEntity), methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static void Warning(string message, params object[] values)
        {
            Log.Warning(message, values);
        }

        public static void Verbose(string message, params object[] values)
        {
            Log.Verbose(message, values);
        }

    }

}
