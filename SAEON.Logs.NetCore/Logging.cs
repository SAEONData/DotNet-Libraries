using Microsoft.Extensions.Configuration;
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
        public static LogEventLevel Level
        {
            get
            {
                foreach (LogEventLevel logLevel in Enum.GetValues(typeof(LogEventLevel)))
                {
                    if (Log.IsEnabled(logLevel)) return logLevel;
                }
                // Shouldn’t get here!
                return LogEventLevel.Information;
            }
        }

        public static LoggerConfiguration CreateConfiguration(string fileName = "", IConfiguration config = null)
        {
            var result = new LoggerConfiguration()
                                .MinimumLevel.Information()
                                .Enrich.FromLogContext()
                                .WriteTo.Console()
                                .WriteTo.Seq("http://localhost:5341/");
            if (string.IsNullOrWhiteSpace(fileName)) fileName = Path.Combine("Logs", ApplicationHelper.ApplicationName + "-.log");
            result.WriteTo.File(fileName, rollingInterval: RollingInterval.Day, retainedFileCountLimit: null, rollOnFileSizeLimit: true);
#if !NETSTANDARD2_1
            if (config is null)
            {
                config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true)
                    .AddJsonFile("local.settings.json", true)
                    .AddJsonFile("secrets.json", true)
                    .Build();
            }
#endif
            if (config is not null)
            {
                result.ReadFrom.Configuration(config);
            }
            return result;
        }

        public static LoggerConfiguration CreateConfiguration(string fileName)
        {
            return CreateConfiguration(fileName, null);
        }

        public static LoggerConfiguration CreateConfiguration(IConfiguration config)
        {
            return CreateConfiguration(null, config);
        }

        public static void Initialize(this LoggerConfiguration config) => Log.Logger = config.CreateLogger();

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

        public static void Verbose(string message, params object[] values)
        {
            Log.Verbose(message, values);
        }

        public static void Warning(string message, params object[] values)
        {
            Log.Warning(message, values);
        }

        #region MethodCalls
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
        #endregion
    }

}
