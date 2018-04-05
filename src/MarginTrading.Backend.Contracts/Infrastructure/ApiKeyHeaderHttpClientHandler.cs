using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts.Infrastructure
{
    internal class ApiKeyHeaderHttpClientHandler : DelegatingHandler
    {
        private readonly string _apiKey;

        public ApiKeyHeaderHttpClientHandler(HttpMessageHandler innerHandler, string apiKey)
            : base(innerHandler)
        {
            _apiKey = apiKey;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.UserAgent.Clear();
            request.Headers.TryAddWithoutValidation("api-key", _apiKey);
            return base.SendAsync(request, cancellationToken);
        }
    }
}