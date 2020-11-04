using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            return services;
        }
    }
}
