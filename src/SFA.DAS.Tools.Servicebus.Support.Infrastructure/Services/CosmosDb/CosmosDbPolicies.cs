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
    public class CosmosDbPolicies : ICosmosDbPolicies
    {
        public IAsyncPolicy ResiliencePolicy { get; }
        public IAsyncPolicy BulkBatchPolicy { get; }

        private readonly ILogger<CosmosDbPolicies> _logger;

        private const int defaultCosmosOperationTimeout = 55;
        private const int defaultCosmosInterimRequestTimeout = 2;

        public CosmosDbPolicies(CosmosDbSettings cosmosDbSettings, ILogger<CosmosDbPolicies> logger)
        {
            _logger = logger;
            var cosmosDbOperationTimeout = 
                cosmosDbSettings.DefaultCosmosOperationTimeout <= 0 ? defaultCosmosOperationTimeout : cosmosDbSettings.DefaultCosmosInterimRequestTimeout;

            var cosmosDbInterimRequestTimeout = 
                cosmosDbSettings.DefaultCosmosInterimRequestTimeout <= 0 ? defaultCosmosInterimRequestTimeout : cosmosDbSettings.DefaultCosmosInterimRequestTimeout;

            // Handle CosmosException Only,
            // If the status code is not 429, try 3 times with 2 seconds inbetween by default,
            // If it is 429, inspect the retry after value and retry then.
            ResiliencePolicy = Policy.Handle<CosmosException>()
                .WaitAndRetryAsync(3, sleepDurationProvider: (retryCount, response, context) =>
                {
                    if (response is CosmosException cosmosException 
                    && cosmosException.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    && cosmosException.RetryAfter.HasValue)
                    {
                        return cosmosException.RetryAfter.Value;
                    }

                    return TimeSpan.FromSeconds(cosmosDbInterimRequestTimeout);

                }, onRetryAsync: async (exception, timeSpan, rretryCount, pollyContext) => 
                {
                    logger.LogWarning(exception, $"Error accessing CosmosDb, Reason: {exception?.Message}. Retrying in {timeSpan.Seconds} secs...");
                    await Task.CompletedTask;
                });

            // Some calls have a static time before they have to fail due to peeklock demands of 60 seconds tiemout
            // Therefore, don't let the request exceed 60 seconds so it shorts and returns the peeklock.
            var timeoutPolicy = Policy.TimeoutAsync(cosmosDbOperationTimeout, onTimeoutAsync: async (context, timespan, task) =>
                {
                    await task?.ContinueWith(t =>
                    {
                        if (t.IsFaulted) _logger.LogError($"{context.PolicyKey} at {context.OperationKey}: execution timed out after {timespan.TotalSeconds} seconds, with: {t.Exception}.");
                    });
                });

            BulkBatchPolicy = timeoutPolicy.WrapAsync(ResiliencePolicy);
        }
    }
}
