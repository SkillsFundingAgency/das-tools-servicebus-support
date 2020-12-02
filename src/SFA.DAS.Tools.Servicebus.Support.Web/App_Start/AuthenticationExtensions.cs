using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SFA.DAS.Tools.Servicebus.Support.Web.Configuration;

namespace SFA.DAS.Tools.Servicebus.Support.Web
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var azureAdConfiguration = new AzureAdConfiguration();
            configuration.GetSection("AzureAdConfiguration").Bind(azureAdConfiguration);

            services.AddAuthentication(options =>
                {
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                }).AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = azureAdConfiguration.Authority;
                    options.ClientId = azureAdConfiguration.ClientId;
                    options.ClientSecret = azureAdConfiguration.ClientSecret;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                }).AddCookie(options =>
                { 
                    options.AccessDeniedPath = new PathString("/Error/403");
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                    options.Cookie.Name = "SFA.DAS.ToolServicebus.Support.Web.Auth";
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.SlidingExpiration = true;
                });

            return services;
        }
    }
}
