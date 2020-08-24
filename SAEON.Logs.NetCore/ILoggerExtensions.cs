using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SAEON.Logs
{
    public static class ILoggerExtensions
    {
        public static bool UseSAEONLogs { get; set; }

        public static LogLevel Level(this ILogger logger)
        {
            foreach (var level in Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>())
            {
                if (logger.IsEnabled(level)) return level;
            }
            // Shouldn’t get here!
            throw new InvalidOperationException("Unknown LogLevel");
        }

        public static LogLevel Level<T>(this ILogger<T> logger)
        {
            foreach (var level in Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>())
            {
                if (logger.IsEnabled(level)) return level;
            }
            // Shouldn’t get here!
            throw new InvalidOperationException("Unknown LogLevel");
        }

        public static void Debug(this ILogger logger, string message = "", params object[] values)
        {
            logger.LogDebug(message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Debug(message, values);
            }
        }

        public static void Debug<T>(this ILogger<T> logger, string message = "", params object[] values)
        {
            logger.LogDebug(message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Debug(message, values);
            }
        }

        public static void Error(this ILogger logger, string message = "", params object[] values)
        {
            logger.LogError(string.IsNullOrEmpty(message) ? "An error occurred" : message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Error(message, values);
            }
        }

        public static void Error<T>(this ILogger<T> logger, string message = "", params object[] values)
        {
            logger.LogError(string.IsNullOrEmpty(message) ? "An error occurred" : message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Error(message, values);
            }
        }

        public static void Exception(this ILogger logger, Exception ex, string message = "", params object[] values)
        {
            logger.LogError(ex, string.IsNullOrEmpty(message) ? "An exception occurred" : message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Exception(ex, message, values);
            }
        }

        public static void Exception<T>(this ILogger<T> logger, Exception ex, string message = "", params object[] values)
        {
            logger.LogError(ex, string.IsNullOrEmpty(message) ? "An exception occurred" : message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Exception(ex, message, values);
            }
        }

        public static void Fatal(this ILogger logger, string message = "", params object[] values)
        {
            logger.LogCritical(string.IsNullOrEmpty(message) ? "A fatal error occurred" : message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Error(message, values);
            }
        }

        public static void Fatal<T>(this ILogger<T> logger, string message = "", params object[] values)
        {
            logger.LogCritical(string.IsNullOrEmpty(message) ? "A fatal error occurred" : message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Error(message, values);
            }
        }

        public static void Information(this ILogger logger, string message, params object[] values)
        {
            logger.LogInformation(message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Information(message, values);
            }
        }

        public static void Information<T>(this ILogger<T> logger, string message, params object[] values)
        {
            logger.LogInformation(message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Information(message, values);
            }
        }

        public static void Verbose(this ILogger logger, string message, params object[] values)
        {
            logger.LogTrace(message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Verbose(message, values);
            }
        }

        public static void Verbose<T>(this ILogger<T> logger, string message, params object[] values)
        {
            logger.LogTrace(message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Verbose(message, values);
            }
        }

        public static void Warning(this ILogger logger, string message, params object[] values)
        {
            logger.LogWarning(message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Warning(message, values);
            }
        }

        public static void Warning<T>(this ILogger<T> logger, string message, params object[] values)
        {
            logger.LogWarning(message, values);
            if (UseSAEONLogs)
            {
                SAEONLogs.Warning(message, values);
            }
        }

        #region MethodCalls
        public static IDisposable MethodCall(this ILogger logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            logger.LogDebug(MethodCalls.MethodSignature(type, methodName, parameters));
            return SAEONLogs.MethodCall(type, parameters, methodName);
        }

        public static IDisposable MethodCall<T>(this ILogger<T> logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            logger.LogDebug(MethodCalls.MethodSignature(type, methodName, parameters));
            return SAEONLogs.MethodCall(type, parameters, methodName);
        }

        public static IDisposable MethodCall<TEntity>(this ILogger logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            logger.LogDebug(MethodCalls.MethodSignature(type, typeof(TEntity), methodName, parameters));
            return SAEONLogs.MethodCall<TEntity>(type, parameters, methodName);
        }

        public static IDisposable MethodCall<T, TEntity>(this ILogger<T> logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            logger.LogDebug(MethodCalls.MethodSignature(type, typeof(TEntity), methodName, parameters));
            return SAEONLogs.MethodCall<TEntity>(type, parameters, methodName);
        }

        public static IDisposable MethodCall<TEntity, TRelatedEntity>(this ILogger logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            logger.LogDebug(MethodCalls.MethodSignature(type, typeof(TEntity), typeof(TRelatedEntity), methodName, parameters));
            return SAEONLogs.MethodCall<TEntity, TRelatedEntity>(type, parameters, methodName);
        }

        public static IDisposable MethodCall<T, TEntity, TRelatedEntity>(this ILogger<T> logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            logger.LogDebug(MethodCalls.MethodSignature(type, typeof(TEntity), typeof(TRelatedEntity), methodName, parameters));
            return SAEONLogs.MethodCall<TEntity, TRelatedEntity>(type, parameters, methodName);
        }
        #endregion
    }
}
