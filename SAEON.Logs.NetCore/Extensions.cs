using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SAEON.Core;
using Serilog;
using Serilog.Extensions.Logging;
using System; 
using System.IO;

namespace SAEON.Logs
{
    public static class SAEONLogsExtensions
    {
        public static IHostBuilder UseSAEONLogs(this IHostBuilder builder, ILogger logger = null, bool dispose = false, LoggerProviderCollection providers = null)
        {
            builder.UseSerilog(logger, dispose, providers);
            return builder;
        }

        public static IHostBuilder UseSAEONLogs(this IHostBuilder builder, Action<HostBuilderContext, LoggerConfiguration> configureLogger, bool preserveStaticLogger = false, bool writeToProviders = false)
        {
            builder.UseSerilog(configureLogger, preserveStaticLogger, writeToProviders);
            return builder;
        }

        public static LoggerConfiguration InitializeSAEONLogs(this LoggerConfiguration loggerConfiguration, IConfiguration config, string fileName = "")
        {
            if (config != null) loggerConfiguration.ReadFrom.Configuration(config);
            loggerConfiguration.Enrich.FromLogContext();
            loggerConfiguration.WriteTo.Seq("http://localhost:5341/");
            if (string.IsNullOrWhiteSpace(fileName)) fileName = Path.Combine("Logs", ApplicationHelper.ApplicationName + ".txt");
            if (!string.IsNullOrWhiteSpace(fileName)) loggerConfiguration.WriteTo.File(fileName, rollOnFileSizeLimit: true, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1), rollingInterval: RollingInterval.Day, retainedFileCountLimit: null);
            return loggerConfiguration;
        }

    }
}
