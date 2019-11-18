using SAEON.Core;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace SAEON.Logs
{
#pragma warning disable CA2237 // Mark ISerializable types with serializable
    public class ParameterList : Dictionary<string, object> { }
#pragma warning restore CA2237 // Mark ISerializable types with serializable

    public static class Logging
    {
        public static bool UseFullName { get; set; } = true;

        public static string LogLevel =>
            Log.IsEnabled(LogEventLevel.Verbose) ? "Verbose" :
            Log.IsEnabled(LogEventLevel.Debug) ? "Debug" :
            Log.IsEnabled(LogEventLevel.Information) ? "Info" :
            Log.IsEnabled(LogEventLevel.Warning) ? "Warning" :
            Log.IsEnabled(LogEventLevel.Error) ? "Error" :
            Log.IsEnabled(LogEventLevel.Fatal) ? "Fatal" :
            "Unknown";

        public static LoggerConfiguration CreateConfiguration(string fileName = "")
        {
            var result = new LoggerConfiguration()
                            .ReadFrom.AppSettings()
                            .Enrich.FromLogContext()
                            .WriteTo.Seq("http://localhost:5341/");
            if (string.IsNullOrWhiteSpace(fileName)) fileName = Path.Combine("Logs", ApplicationHelper.ApplicationName+".txt");
            if (!string.IsNullOrWhiteSpace(fileName)) result.WriteTo.RollingFile(fileName);
            return result;
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

        public static void Debug(string message = "", params object[] values)
        {
            Log.Debug(message, values);
        }

        public static void Error(string message = "", params object[] values)
        {
            Log.Error(string.IsNullOrEmpty(message) ? "An error occurred" : message, values);
        }

        public static void Exception(Exception ex, string message = "", params object[] values)
        {
            Log.Error(ex, string.IsNullOrEmpty(message) ? "An exception occurred" : message, values);
        }

        public static void Fatal(string message = "", params object[] values)
        {
            Log.Fatal(string.IsNullOrEmpty(message) ? "A fatal error occurred" : message, values);
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

        private static string GetParameters(ParameterList parameters)
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

        public static string MethodSignature(Type type, string methodName, ParameterList parameters = null)
        {
            return $"{GetTypeName(type)}.{methodName}({GetParameters(parameters)})".Replace("..",".");
        }

        public static string MethodSignature(Type type, Type entityType, string methodName, ParameterList parameters = null)
        {
            return $"{GetTypeName(type)}<{GetTypeName(entityType)}>.{methodName}({GetParameters(parameters)})".Replace("..", ".");
        }

        public static string MethodSignature(Type type, Type entityType, Type relatedEntityType, string methodName, ParameterList parameters = null)
        {
            return $"{GetTypeName(type)}<{GetTypeName(entityType)},{GetTypeName(relatedEntityType)}>.{methodName}({GetParameters(parameters)})".Replace("..", ".");
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
            var method = MethodSignature(type, typeof(TEntity), methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity, TRelatedEntity>(Type type, ParameterList parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodSignature(type, typeof(TEntity), typeof(TRelatedEntity), methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static void Verbose(string message, params object[] values)
        {
            Log.Verbose(message, values);
        }

        public static void Warning(string message, params object[] values)
        {
            Log.Warning(message, values);
        }
    }

}