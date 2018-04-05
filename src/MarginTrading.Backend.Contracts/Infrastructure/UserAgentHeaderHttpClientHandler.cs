using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts.Infrastructure
{
    internal class UserAgentHeaderHttpClientHandler : DelegatingHandler
    {
        private readonly string _userAgent;

        public UserAgentHeaderHttpClientHandler(HttpMessageHandler innerHandler, string userAgent)
            : base(innerHandler)
        {
            _userAgent = userAgent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.UserAgent.Clear();
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            return base.SendAsync(request, cancellationToken);
        }
    }
}