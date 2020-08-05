using SAEON.Core;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SAEON.Logs
{
    public static class SAEONLogs
    {
        public static bool UseFullName { get; set; } = true;

        public static LogEventLevel Level
        {
            get
            {
                foreach (LogEventLevel logLevel in Enum.GetValues(typeof(LogEventLevel)))
                {
                    if (Log.IsEnabled(logLevel)) return logLevel;
                }
                return LogEventLevel.Information;
                // Shouldn’t get here!
            }
        }

        public static LoggerConfiguration CreateConfiguration(string fileName = "")
        {
            var result = new LoggerConfiguration()
                            .ReadFrom.AppSettings()
                            .Enrich.FromLogContext()
                            .WriteTo.Seq("http://localhost:5341/");
            if (string.IsNullOrWhiteSpace(fileName)) fileName = Path.Combine("Logs", ApplicationHelper.ApplicationName + ".txt");
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

        public static IDisposable MethodCall(Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodCalls.MethodSignature(type, methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity>(Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodCalls.MethodSignature(type, typeof(TEntity), methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            Log.Verbose(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity, TRelatedEntity>(Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodCalls.MethodSignature(type, typeof(TEntity), typeof(TRelatedEntity), methodName, parameters);
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