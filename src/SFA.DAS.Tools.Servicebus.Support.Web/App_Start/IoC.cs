using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class IoC
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration _configuration)
        {
            services.AddTransient<ISvcBusService, SvcBusService>(s => new SvcBusService(_configuration , s.GetRequiredService<ILogger<SvcBusService>>()));            

            return services;
        }
    }
}
