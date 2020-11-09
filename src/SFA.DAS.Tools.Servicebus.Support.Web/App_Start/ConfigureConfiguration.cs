using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Tools.Servicebus.Support.Audit;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class ConfigureConfiguration
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ServiceBusErrorManagementSettings>(configuration.GetSection(ServiceBusErrorManagementSettings.ServiceBusErrorManagementSettingsKey));
            services.Configure<UserIdentitySettings>(configuration.GetSection(UserIdentitySettings.UserIdentitySettingsKey));
            services.Configure<CosmosDbSettings>(configuration.GetSection(CosmosDbSettings.CosmosDbSettingsKey));
            
            // Remove IOptions Abstraction from Configure
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<CosmosDbSettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<UserIdentitySettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ServiceBusErrorManagementSettings>>().Value);

            // Could clash with other Apis, the table storage is all put together
            services.AddTransient<IAuditApiConfiguration>(s => new AuditApiConfiguration
            {
                ApiBaseUrl = configuration.GetValue<string>("ApiBaseUrl"),
                ClientId = configuration.GetValue<string>("ClientId"),
                ClientSecret = configuration.GetValue<string>("ClientSecret"),
                IdentifierUri = configuration.GetValue<string>("IdentifierUri"),
                Tenant = configuration.GetValue<string>("Tenant")
            });

            return services;
        }
    }
}
