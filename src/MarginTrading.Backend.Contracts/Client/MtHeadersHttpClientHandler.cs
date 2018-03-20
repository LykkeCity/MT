using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts.Client
{
    internal class MtHeadersHttpClientHandler : DelegatingHandler
    {
        private readonly string _userAgent;
        private readonly string _apiKey;

        public MtHeadersHttpClientHandler(HttpMessageHandler innerHandler, string userAgent, string apiKey)
            : base(innerHandler)
        {
            _userAgent = userAgent;
            _apiKey = apiKey;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.UserAgent.Clear();
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            request.Headers.TryAddWithoutValidation("api-key", _apiKey);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}