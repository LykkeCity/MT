using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Flurl.Http;
using MarginTrading.Common.Extensions;
using MarginTrading.Core.Models;
using MarginTrading.Frontend.Settings;
using System.Linq;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Frontend.Services
{
    public interface IHttpRequestService
    {
        Task<TResponse> RequestWithRetriesAsync<TResponse>(object request, string action, bool isLive = true, string controller = "mt");

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="path"></param>
        /// <param name="isLive"></param>
        /// <returns></returns>
        Task<TResponse> GetAsync<TResponse>(string path, bool isLive = true);


        /// <summary>
        /// Makes a post requests for available backends for client (live/demo) and gets results.
        /// If a backend is not available for client or request fails - <paramref name="defaultResult"/> is returned instead.
        /// </summary>
        Task<(TResponse Demo, TResponse Live)> RequestIfAvailableAsync<TResponse>(object request, string action, Func<TResponse> defaultResult, EnabledMarginTradingTypes enabledMarginTradingTypes, string controller = "mt")
            where TResponse : class;
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
                    TimeSpan.FromSeconds(5),
                    ex => ProcessException(isLive, action, request.ToJson(), ex, true));
            }
            catch (Exception ex)
            {
                ProcessException(isLive, action, request.ToJson(), ex, false);
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
                ProcessException(isLive, path, "GET", ex, false);
                throw;
            }
        }

        public void ProcessException(bool isLive, string path, string context, Exception ex, bool willBeRetried)
        {
            path = $"{(isLive ? "Live: " : "Demo: ")}{path}";

            void WriteLog(Exception e)
            {
                if (willBeRetried)
                {
                    _log.WriteWarningAsync(nameof(HttpRequestService), path, context,
                        "An exception has been encountered but will be retried: " + e);
                }
                else
                {
                    _log.WriteErrorAsync(nameof(HttpRequestService), path, context, e);
                }
            }

            WriteLog(ex);

            var responseBody = (ex as FlurlHttpException)?.Call.ErrorResponseBody;
            if (!string.IsNullOrEmpty(responseBody))
            {
                var response = responseBody.DeserializeJson<MtBackendResponse<string>>();
                if (!string.IsNullOrEmpty(response?.Message))
                {
                    var newEx = new Exception(response.Message);
                    WriteLog(newEx);
                    throw newEx;
                }
            }
        }
    }
}
