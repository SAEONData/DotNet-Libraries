#if NETCOREAPP2_1 || NETCOREAPP2_2
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.AspNetCore;
#elif NETSTANDARD2_0
using Microsoft.Extensions.Configuration;
#endif

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

#if NETSTANDARD2_0 || NETCOREAPP2_1 || NETCOREAPP2_2
        public static LoggerConfiguration CreateConfiguration(string fileName, IConfiguration config)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext()
                .WriteTo.File(fileName, rollOnFileSizeLimit: true, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1), rollingInterval: RollingInterval.Day, retainedFileCountLimit: null)
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341/");
        }
#else

        public static LoggerConfiguration CreateConfiguration(string fileName)
        {
            return new LoggerConfiguration()
            .ReadFrom.AppSettings()
            .Enrich.FromLogContext()
            .WriteTo.RollingFile(fileName)
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341/");
        }

#endif

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

        public static void Warning(string message, params object[] values)
        {
            Log.Warning(message, values);
        }

        public static void Verbose(string message, params object[] values)
        {
            Log.Verbose(message, values);
        }
    }

#if NETCOREAPP2_1|| NETCOREAPP2_2
    public static class SAEONWebHostExtensions
    {
        public static IWebHostBuilder UseSAEONLogs(this IWebHostBuilder builder, Serilog.ILogger logger = null, bool dispose = false)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            builder.ConfigureServices(collection =>
                collection.AddSingleton<ILoggerFactory>(services => new SerilogLoggerFactory(logger, dispose)));
            return builder;
        }

        public static IWebHostBuilder UseSerilog(this IWebHostBuilder builder, Action<WebHostBuilderContext, LoggerConfiguration> configureLogger, bool preserveStaticLogger = false)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configureLogger == null) throw new ArgumentNullException(nameof(configureLogger));
            builder.ConfigureServices((context, collection) =>
            {
                var loggerConfiguration = new LoggerConfiguration();
                configureLogger(context, loggerConfiguration);
                var logger = loggerConfiguration.CreateLogger();
                if (preserveStaticLogger)
                {
                    collection.AddSingleton<ILoggerFactory>(services => new SerilogLoggerFactory(logger, true));
                }
                else
                {
                    // Passing a `null` logger to `SerilogLoggerFactory` results in disposal via
                    // `Log.CloseAndFlush()`, which additionally replaces the static logger with a no-op.
                    Log.Logger = logger;
                    collection.AddSingleton<ILoggerFactory>(services => new SerilogLoggerFactory(null, true));
                }
            });
            return builder;
        }
    }
#endif
}