# SAEON.Logging #
The South African Environmental Observation Network (SAEON) Logs provides logging for .NetCore

Program.cs

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SAEON.Logs;
using System;

namespace SAEON.Observations.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SAEONLogs.CreateConfiguration().Initialize();
            try
            {
                Logging.Information("Starting application");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
                throw;
            }
            finally
            {
                Logging.ShutDown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSAEONLogs()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
