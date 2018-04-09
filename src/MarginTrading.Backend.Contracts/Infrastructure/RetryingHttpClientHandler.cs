using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace MarginTrading.Backend.Contracts.Infrastructure
{
    internal class RetryingHttpClientHandler : DelegatingHandler
    {
        private readonly RetryPolicy _retryPolicy;

        public RetryingHttpClientHandler(HttpMessageHandler innerHandler, int retryCount, TimeSpan retrySleepDuration)
            : base(innerHandler)
        {
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(retryCount, retryAttempt => retrySleepDuration, 
                    (exception, timeSpan, retryAttempt, context) => context["RetriesLeft"] = retryCount - retryAttempt);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _retryPolicy.ExecuteAsync(async (context, ct) =>
            {
                var response = await base.SendAsync(request, ct);
                if ((!context.TryGetValue("RetriesLeft", out var retriesLeft) || (int)retriesLeft > 0) && 
                    !response.IsSuccessStatusCode &&
                    response.StatusCode != HttpStatusCode.BadRequest &&
                    response.StatusCode != HttpStatusCode.InternalServerError)
                {
                    // throws to execute retry
                    response.EnsureSuccessStatusCode(); 
                }
                
                return response;
            }, ImmutableDictionary<string, object>.Empty, cancellationToken);
        }
    }
}