using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Tools.Servicebus.Support.Functions;

[assembly: FunctionsStartup(typeof(Startup))]
namespace SFA.DAS.Tools.Servicebus.Support.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var sp = builder.Services.BuildServiceProvider();
            var configurationService = sp.GetService<IConfiguration>();

            builder.Services.AddServices(configurationService);
        }
    }
}
