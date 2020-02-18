using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SAEON.Logs
{
    public static class ILoggerExtensions
    {
        public static string Level(this ILogger logger)
        {
            foreach (var level in Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>())
            {
                if (logger.IsEnabled(level)) return level.ToString();
            }
            // Shouldn’t get here!
            return "Unknown";
        }

        public static string Level<T>(this ILogger<T> logger)
        {
            foreach (var level in Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>())
            {
                if (logger.IsEnabled(level)) return level.ToString();
            }
            // Shouldn’t get here!
            return "Unknown";
        }

        //public static void LogError(this ILogger logger, string message = "", params object[] args)
        //{
        //    if (string.IsNullOrWhiteSpace(message)) message = "An error occurred";
        //    logger.LogError(message, args);
        //}

        //public static void LogError<T>(this ILogger<T> logger, string message = "", params object[] args)
        //{
        //    if (string.IsNullOrWhiteSpace(message)) message = "An error occurred";
        //    logger.LogError(message, args);
        //}

        public static void LogException(this ILogger logger, Exception ex, string message = "", params object[] args)
        {
            if (string.IsNullOrWhiteSpace(message)) message = "An exception occurred";
            logger.LogError(ex, message, args);
        }

        public static void LogException<T>(this ILogger<T> logger, Exception ex, string message = "", params object[] args)
        {
            if (string.IsNullOrWhiteSpace(message)) message = "An exception occurred";
            logger.LogError(ex, message, args);
        }

        #region MethodCalls
        public static IDisposable MethodCall(this ILogger logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodCalls.MethodSignature(type, methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            logger.LogDebug(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity>(this ILogger logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodCalls.MethodSignature(type, typeof(TEntity), methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            logger.LogDebug(method);
            return result;
        }

        public static IDisposable MethodCall<TEntity, TRelatedEntity>(this ILogger logger, Type type, MethodCallParameters parameters = null, [CallerMemberName] string methodName = "")
        {
            var method = MethodCalls.MethodSignature(type, typeof(TEntity), typeof(TRelatedEntity), methodName, parameters);
            var result = LogContext.PushProperty("Method", method);
            logger.LogDebug(method);
            return result;
        }
        #endregion

    }

}
