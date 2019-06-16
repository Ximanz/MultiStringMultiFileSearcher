using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace MultiStringMultiFileSearcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = LogManager.GetCurrentClassLogger();

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var servicesProvider = BuildDi(config);

                using (servicesProvider as IDisposable)
                {
                    var runner = servicesProvider.GetRequiredService<MultiStringMultiFileSearcher>();
                    runner.Execute();

                    Console.WriteLine("Press ANY key to exit");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static IServiceProvider BuildDi(IConfiguration config)
        {
            var serviceCollection = new ServiceCollection()
                .AddTransient<MultiStringMultiFileSearcher>() // Runner is the custom class
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(config);

                    var nLogPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");

                    LogManager.KeepVariablesOnReload = true;
                    LogManager.Configuration.Variables["LogHome"] = nLogPath;
                });

            var builder = new ContainerBuilder();
            builder.Populate(serviceCollection);

            AppConfig appConfig = new AppConfig(config);
            builder.RegisterInstance(appConfig).As<AppConfig>();

            var container = builder.Build();
            return new AutofacServiceProvider(container);
        }
    }
}
