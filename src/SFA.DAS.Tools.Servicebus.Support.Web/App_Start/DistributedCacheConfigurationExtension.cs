using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start;

public static class DistributedCacheConfigurationExtension
{
    private const string ApplicationName = "das-tools-servicebus-support";
    public static void AddDistributedCache(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            services.AddDistributedMemoryCache();
            var keysPath = Path.Combine(env.ContentRootPath, "dataprotection-keys");
            Directory.CreateDirectory(keysPath);
            services.AddDataProtection()
                .SetApplicationName(ApplicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
        else
        {
            var redisConnectionString = configuration["RedisConnectionString"];

            var redis = ConnectionMultiplexer.Connect($"{redisConnectionString},DefaultDatabase=0");
            services.AddDataProtection()
                .SetApplicationName(ApplicationName)
                .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
        }
    }
}