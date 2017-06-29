using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Flurl.Http;
using MarginTrading.Common.BackendContracts;
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
                await ProcessException(isLive, action, request.ToJson(), ex);
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
                await ProcessException(isLive, path, "GET", ex);
                throw;
            }
        }

        public async Task ProcessException(bool isLive, string path, string context, Exception ex)
        {
            path = $"{(isLive ? "Live: " : "Demo: ")}{path}";

            await _log.WriteErrorAsync(nameof(HttpRequestService), path, context, ex);

            var flUrlEx = ex as FlurlHttpException;

            var responseBody = flUrlEx?.Call.ErrorResponseBody;

            if (!string.IsNullOrEmpty(responseBody))
            {
                var response = responseBody.DeserializeJson<MtBackendResponse<string>>();
                if (!string.IsNullOrEmpty(response?.Message))
                {
                    var newEx = new Exception(response.Message);
                    await _log.WriteErrorAsync(nameof(HttpRequestService), path, context, newEx);
                    throw newEx;
                }
            }
        }
    }
}
