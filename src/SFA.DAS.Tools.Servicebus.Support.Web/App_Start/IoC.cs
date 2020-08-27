using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class IoC
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration _configuration)
        {
            services.AddTransient<ISvcBusService, SvcBusService>(s => new SvcBusService(_configuration , s.GetRequiredService<ILogger<SvcBusService>>()));            
            services.AddTransient<ICosmosMessageService, CosmosMessageService>(s => new CosmosMessageService(s.GetRequiredService<CosmosClient>(), _configuration, s.GetRequiredService<ILogger<CosmosMessageService>>()));

            services.AddSingleton(s =>
            {
                var cosmosEnpointUrl = _configuration.GetValue<string>("CosmosDb:Url");
                var cosmosAuthenticationKey = _configuration.GetValue<string>("CosmosDb:AuthKey");
                
                return new CosmosClient(cosmosEnpointUrl, cosmosAuthenticationKey, new CosmosClientOptions() { AllowBulkExecution = true });
            });

            return services;
        }
    }
}
