using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Polly;
using Polly.Retry;

namespace MarginTrading.Backend.Contracts.Infrastructure
{
    internal class RetryingHttpClientHandler : DelegatingHandler
    {
        private readonly RetryPolicy _retryPolicy;

        public RetryingHttpClientHandler([NotNull] HttpMessageHandler innerHandler, int retryCount,
            [NotNull] Func<int, string, TimeSpan> sleepDurationProvider)
            : base(innerHandler)
        {
            if (innerHandler == null) throw new ArgumentNullException(nameof(innerHandler));
            if (sleepDurationProvider == null) throw new ArgumentNullException(nameof(sleepDurationProvider));
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount));
            
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(retryCount, 
                    (retryAttempt, context) => sleepDurationProvider(retryAttempt, context.ExecutionKey),
                    (exception, timeSpan, retryAttempt, context) => context["RetriesLeft"] = retryCount - retryAttempt);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _retryPolicy.ExecuteAsync(async (context, ct) =>
            {
                var response = await base.SendAsync(request, ct);
                if ((!context.TryGetValue("RetriesLeft", out var retriesLeft) || (int) retriesLeft > 0) &&
                    !response.IsSuccessStatusCode &&
                    response.StatusCode != HttpStatusCode.BadRequest &&
                    response.StatusCode != HttpStatusCode.InternalServerError)
                {
                    // throws to execute retry
                    response.EnsureSuccessStatusCode();
                }

                return response;
            }, new Context(request.RequestUri.ToString()), cancellationToken);
        }
    }
}