using System;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions
{
    public static class PolicyRegistryExtensions
    {
        private const int RetryCount = 3;
        private const int Timeout = 200;

        public static void ConfigureWaitAndRetry(this PolicyRegistry policyRegistry, string policyKey, ILogger logger)
        {
            if (policyRegistry.ContainsKey(policyKey))
            {
                return;
            }

            var policy = Policy.Handle<Exception>().WaitAndRetryAsync(RetryCount, attempt => TimeSpan.FromMilliseconds(Timeout),
                (exception, timeSpan, pollyContext) =>
                {
                    logger.LogWarning(exception, $"Error executing command for method {pollyContext.PolicyKey} " +
                                                 $"Reason: {exception?.Message}. " +
                                                 $"Retrying in {timeSpan.Seconds} secs...");
                });

            policyRegistry.Add(policyKey, policy);
        }
    }
}
