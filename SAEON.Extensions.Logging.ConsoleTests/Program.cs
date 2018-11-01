using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace SAEON.Extensions.Logging.ConsoleTests
{
    class Program
    {
        private static void ConfigureServices(IServiceCollection services)
        {
            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Information);

            services.AddSingleton(loggerFactory); // Add first my already configured instance
            services.AddLogging(); // Allow ILogger<T>

            services.AddTransient<Program>();
        }

        static void Main(string[] args)
        {
            try
            {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                using (var serviceProvider = serviceCollection.BuildServiceProvider())
                {
                    var log = serviceProvider.GetService<ILogger<Program>>();
                    log.LogInformation("City: {city} Country: {country}", null, "test");
                    log.Information("Main","City: {city} Country: {country}", null, "test");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}
