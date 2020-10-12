using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using SFA.DAS.Configuration.AzureTableStorage;
using SFA.DAS.Tools.Servicebus.Support.Functions;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace SFA.DAS.Tools.Servicebus.Support.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var sp = builder.Services.BuildServiceProvider();

            var executionContextOptions = builder.Services.BuildServiceProvider()
                .GetService<IOptions<ExecutionContextOptions>>()
                .Value;
            var appDirectory = executionContextOptions.AppDirectory;
            var configurationService = sp.GetService<IConfiguration>();
            var configuration = LoadConfiguration(appDirectory, configurationService);

            builder.Services.AddServices(configuration);

            var nLogConfiguration = new NLogConfiguration();
            builder.Services.AddLogging((options) =>
            {
                options.SetMinimumLevel(LogLevel.Trace);
                options.SetMinimumLevel(LogLevel.Trace);
                options.AddNLog(new NLogProviderOptions
                {
                    CaptureMessageTemplates = true,
                    CaptureMessageProperties = true
                });
                options.AddConsole();

                nLogConfiguration.ConfigureNLog(configurationService);
            });
        }

        private IConfiguration LoadConfiguration(string appDirectory, IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(appDirectory)
               .AddJsonFile("local.settings.json",
                   optional: true,
                   reloadOnChange: true)
               .AddConfiguration(configuration);

            if (!configuration["Environment"].Equals("DEV", StringComparison.CurrentCultureIgnoreCase))
            {
                builder.AddAzureTableStorage(options =>
                {
                    options.ConfigurationKeys = configuration["ConfigNames"].Split(",");
                    options.StorageConnectionString = configuration["ConfigurationStorageConnectionString"];
                    options.EnvironmentName = configuration["Environment"];
                    options.PreFixConfigurationKeys = false;
                }
                );
            }

            return builder.Build();
        }


    }
}
