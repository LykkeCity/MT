using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Flurl.Http;
using MarginTrading.Frontend.Settings;

namespace MarginTrading.Frontend.Services
{
    public class HttpRequestService
    {
        private readonly MtFrontendSettings _settings;
        private readonly ILog _log;

        public HttpRequestService(MtFrontendSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        public async Task<TResponse> RequestAsync<TResponse>(object request, string action, bool isLive = true)
        {
            try
            {
                return await $"{(isLive ? _settings.MarginTradingLive.ApiRootUrl : _settings.MarginTradingDemo.ApiRootUrl)}/api/mt/{action}"
                    .WithHeader("api-key", isLive ? _settings.MarginTradingLive.ApiKey : _settings.MarginTradingDemo.ApiKey)
                    .PostJsonAsync(request)
                    .ReceiveJson<TResponse>();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(HttpRequestService), action, request.ToJson(), ex);
                throw;
            }
        }
    }
}
