using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Flurl.Http;
using MarginTrading.Frontend.Settings;

namespace MarginTrading.Frontend.Services
{
    public interface IHttpRequestService
    {
        Task<TResponse> RequestAsync<TResponse>(object request, string action, bool isLive = true, string controller = "mt");
        Task<TResponse> GetAsync<TResponse>(string path, bool isLive = true);
    }

    public class HttpRequestService : IHttpRequestService
    {
        private readonly MtFrontendSettings _settings;
        private readonly ILog _log;

        public HttpRequestService(MtFrontendSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        public async Task<TResponse> RequestAsync<TResponse>(object request, string action, bool isLive = true, string controller = "mt")
        {
            try
            {
                return await $"{(isLive ? _settings.MarginTradingLive.ApiRootUrl : _settings.MarginTradingDemo.ApiRootUrl)}/api/{controller}/{action}"
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

        public async Task<TResponse> GetAsync<TResponse>(string path, bool isLive = true)
        {
            try
            {
                return await $"{(isLive ? _settings.MarginTradingLive.ApiRootUrl : _settings.MarginTradingDemo.ApiRootUrl)}/api/{path}"
                    .WithHeader("api-key", isLive ? _settings.MarginTradingLive.ApiKey : _settings.MarginTradingDemo.ApiKey)
                    .GetJsonAsync<TResponse>();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(HttpRequestService), path, "GET", ex);
                throw;
            }
        }
    }
}
