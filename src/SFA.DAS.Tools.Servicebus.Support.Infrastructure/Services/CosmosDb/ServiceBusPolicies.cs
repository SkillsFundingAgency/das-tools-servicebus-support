using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class ServiceBusPolicies : IServiceBusPolicies
    {
        private const int RetryCount = 3;
        private const int Timeout = 200;

        public IAsyncPolicy ResiliencePolicy { get; }
        private readonly ILogger<ServiceBusPolicies> _logger;

        public ServiceBusPolicies(ILogger<ServiceBusPolicies> logger)
        {
            _logger = logger;
            
            ResiliencePolicy = Policy.Handle<Exception>().WaitAndRetryAsync(RetryCount, attempt => TimeSpan.FromMilliseconds(Timeout),
                async (exception, timeSpan, pollyContext) =>
                {
                    logger.LogWarning(exception, $"Error executing command for method {pollyContext.PolicyKey} " +
                                                 $"Reason: {exception?.Message}. " +
                                                 $"Retrying in {timeSpan.Seconds} secs...");

                    await Task.CompletedTask;
                });
        }
    }
}
