using System;
using System.Threading.Tasks;
using Common;
using Flurl.Http;
using MarginTrading.Common.Extensions;
using MarginTrading.Frontend.Settings;
using System.Linq;
using System.Net;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Frontend.Repositories.Contract;
using Rocks.Caching;

namespace MarginTrading.Frontend.Services
{
    public class HttpRequestService : IHttpRequestService
    {
        private readonly MtFrontendSettings _settings;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMaintenanceInfoRepository _maintenanceInfoRepository;
        private readonly ITerminalInfoService _terminalInfoService;

        public HttpRequestService(MtFrontendSettings settings, 
            ICacheProvider cacheProvider,
            IMaintenanceInfoRepository maintenanceInfoRepository,
            ITerminalInfoService terminalInfoService)
        {
            _settings = settings;
            _cacheProvider = cacheProvider;
            _maintenanceInfoRepository = maintenanceInfoRepository;
            _terminalInfoService = terminalInfoService;
        }

        public async Task<(TResponse Demo, TResponse Live)> RequestIfAvailableAsync<TResponse>(object request, string action, Func<TResponse> defaultResult, EnabledMarginTradingTypes enabledMarginTradingTypes, string controller = "mt")
            where TResponse : class
        {
            async Task<TResponse> Request(bool isLive, bool isTradingEnabled)
            {
                var maintenanceInfo = await GetMaintenance(isLive);
                var terminalInfo = _terminalInfoService.Get();
                var enabledForTerminal = isLive ? terminalInfo.LiveEnabled : terminalInfo.DemoEnabled;
                
                if (!isTradingEnabled || maintenanceInfo.IsEnabled || !enabledForTerminal)
                {
                    return defaultResult();
                }

                return await RequestWithRetriesAsync<TResponse>(request, action, isLive, controller)
                    .ContinueWith(t => t.IsFaulted ? defaultResult() : t.Result);
            }

            return (Demo: await Request(false, enabledMarginTradingTypes.Demo),
                    Live: await Request(true, enabledMarginTradingTypes.Live));
        }

        public async Task<TResponse> RequestWithRetriesAsync<TResponse>(object request, string action, bool isLive = true, string controller = "mt")
        {
            await CheckMaintenance(isLive);
            
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

        public async Task<TResponse> GetAsync<TResponse>(string path, bool isLive = true, int timeout = 30)
        {
            await CheckMaintenance(isLive);
            
            try
            {
                return await $"{(isLive ? _settings.MarginTradingLive.ApiRootUrl : _settings.MarginTradingDemo.ApiRootUrl)}/api/{path}"
                    .WithHeader("api-key", isLive ? _settings.MarginTradingLive.ApiKey : _settings.MarginTradingDemo.ApiKey)
                    .WithTimeout(timeout)
                    .GetJsonAsync<TResponse>();
            }
            catch (Exception ex)
            {
                throw new Exception(GetErrorMessage(isLive, path, "GET", ex));
            }
        }
        
        
        #region Helpers

        private string GetErrorMessage(bool isLive, string path, string context, Exception ex)
        {
            path = $"{(isLive ? "Live: " : "Demo: ")}{path}";

            var error = ex.Message;

            if (ex is FlurlHttpException flurException)
            {
                var responseBody = flurException.Call.ErrorResponseBody;
                
                if (!string.IsNullOrEmpty(responseBody))
                {
                    var response = responseBody.DeserializeJson<MtBackendResponse<string>>();
                    if (!string.IsNullOrEmpty(response?.Message))
                    {
                        error += " " + response.Message;
                    }
                }

                if (flurException.Call.HttpStatus == HttpStatusCode.ServiceUnavailable)
                {
                    ClearMaintenanceCache(isLive);
                }
            }

           return $"Backend {path} request failed. Error: {error}. Payload: {context}.";
        }

        private async Task<IMaintenanceInfo> GetMaintenance(bool isLive)
        {
            var cacheKey = GetMaintenanceModeCacheKey(isLive);

            return await _cacheProvider.GetAsync(cacheKey,
                async () => new CachableResult<IMaintenanceInfo>(
                    await _maintenanceInfoRepository.GetMaintenanceInfo(isLive),
                    CachingParameters.FromSeconds(15)));
        }
        
        private async Task CheckMaintenance(bool isLive)
        {
            var maintenanceInfo = await GetMaintenance(isLive);
            
            if (maintenanceInfo?.IsEnabled == true)
                throw new MaintenanceException(maintenanceInfo.ChangedDate);
        }

        private string GetMaintenanceModeCacheKey(bool isLive)
        {
            return CacheKeyBuilder.Create(nameof(HttpRequestService), nameof(CheckMaintenance), isLive);
        }

        private void ClearMaintenanceCache(bool isLive)
        {
            _cacheProvider.Remove(GetMaintenanceModeCacheKey(isLive));
        }

        #endregion

    }
}
