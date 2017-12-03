using System;
using System.Threading.Tasks;
using Common;
using Flurl.Http;
using MarginTrading.Common.Extensions;
using MarginTrading.Frontend.Settings;
using System.Linq;
using MarginTrading.Common.Settings.Models;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Frontend.Services
{
    public class HttpRequestService : IHttpRequestService
    {
        private readonly MtFrontendSettings _settings;

        public HttpRequestService(MtFrontendSettings settings)
        {
            _settings = settings;
        }

        public async Task<(TResponse Demo, TResponse Live)> RequestIfAvailableAsync<TResponse>(object request, string action, Func<TResponse> defaultResult, EnabledMarginTradingTypes enabledMarginTradingTypes, string controller = "mt")
            where TResponse : class
        {
            Task<TResponse> Request(bool isLive, bool isTradingEnabled)
            {
                if (!isTradingEnabled)
                {
                    return Task.FromResult(defaultResult());
                }

                return RequestWithRetriesAsync<TResponse>(request, action, isLive, controller).ContinueWith(t => t.IsFaulted ? defaultResult() : t.Result);
            }

            return (Demo: await Request(false, enabledMarginTradingTypes.Demo), Live: await Request(true, enabledMarginTradingTypes.Live));
        }

        public async Task<TResponse> RequestWithRetriesAsync<TResponse>(object request, string action, bool isLive = true, string controller = "mt")
        {
            try
            {
                var flurlClient = $"{(isLive ? _settings.MarginTradingLive.ApiRootUrl : _settings.MarginTradingDemo.ApiRootUrl)}/api/{controller}/{action}"
                    .WithHeader("api-key", isLive ? _settings.MarginTradingLive.ApiKey : _settings.MarginTradingDemo.ApiKey);

                return await ActionExtensions.RetryOnExceptionAsync(
                    () => flurlClient.PostJsonAsync(request).ReceiveJson<TResponse>(),
                    ex => ex is FlurlHttpException && !new int?[] {400, 500}.Contains((int?) ((FlurlHttpException) ex).Call.HttpStatus),
                    6,
                    TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                throw new Exception(GetErrorMessage(isLive, action, request.ToJson(), ex));
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
                throw new Exception(GetErrorMessage(isLive, path, "GET", ex));
            }
        }

        public string GetErrorMessage(bool isLive, string path, string context, Exception ex)
        {
            path = $"{(isLive ? "Live: " : "Demo: ")}{path}";

            var error = ex.Message;
            
            var responseBody = (ex as FlurlHttpException)?.Call.ErrorResponseBody;
            if (!string.IsNullOrEmpty(responseBody))
            {
                var response = responseBody.DeserializeJson<MtBackendResponse<string>>();
                if (!string.IsNullOrEmpty(response?.Message))
                {
                    error += " " + response.Message;
                }
            }

           return $"Backend {path} request failed. Error: {error}. Payload: {context}.";
        }
    }
}
