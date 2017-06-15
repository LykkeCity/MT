using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Common.Wamp;
using MarginTrading.Core;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Settings;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using IsAliveResponse = MarginTrading.Common.ClientContracts.IsAliveResponse;

namespace MarginTrading.Frontend
{
    public class RpcMtFrontend : IRpcMtFrontend
    {
        private readonly MtFrontendSettings _settings;
        private readonly IClientTokenService _clientTokenService;
        private readonly HttpRequestService _httpRequestService;
        private readonly IMarginTradingSettingsService _marginTradingSettingsService;

        public RpcMtFrontend(
            MtFrontendSettings settings,
            IClientTokenService clientTokenService,
            HttpRequestService httpRequestService,
            IMarginTradingSettingsService marginTradingSettingsService)
        {
            _settings = settings;
            _clientTokenService = clientTokenService;
            _httpRequestService = httpRequestService;
            _marginTradingSettingsService = marginTradingSettingsService;
        }

        #region Service

        public IsAliveResponse IsAlive()
        {
            return new IsAliveResponse
            {
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = _settings.MarginTradingFront.Env
            };
        }

        #endregion

        #region Init data

        public async Task<InitDataLiveDemoClientResponse> InitData(string token)
        {
            var clientId = await GetClientId(token);

            var marginTradingDemoEnabled = await _marginTradingSettingsService.IsMargingTradingDemoEnabled(clientId);
            var marginTradingLiveEnabled = await _marginTradingSettingsService.IsMargingTradingLiveEnabled(clientId);

            if (!marginTradingDemoEnabled && !marginTradingLiveEnabled)
            {
                throw new Exception("Margin trading is not available");
            }

            var initDataBackendRequest = new ClientIdBackendRequest { ClientId = clientId };

            var initData = new InitDataLiveDemoClientResponse();
            var assetsLive = await _httpRequestService.RequestAsync<MarginTradingAssetBackendContract[]>(null,
                                 "init.assets") ?? new MarginTradingAssetBackendContract[0];
            var assetsDemo = await _httpRequestService.RequestAsync<MarginTradingAssetBackendContract[]>(null,
                                 "init.assets", false) ?? new MarginTradingAssetBackendContract[0];

            initData.Assets = assetsLive.Concat(assetsDemo).Select(item => item.ToClientContract()).ToArray();

            if (marginTradingLiveEnabled)
            {
                var initDataLiveResponse = await _httpRequestService.RequestAsync<InitDataBackendResponse>(
                    initDataBackendRequest, "init.data");

                initData.Live = initDataLiveResponse.ToClientContract();
            }

            if (marginTradingDemoEnabled)
            {
                var initDataDemoResponse = await _httpRequestService.RequestAsync<InitDataBackendResponse>(
                    initDataBackendRequest, "init.data", false);

                initData.Demo = initDataDemoResponse.ToClientContract();
            }

            return initData;
        }

        public async Task<InitAccountsLiveDemoClientResponse> InitAccounts(string token)
        {
            var clientId = await GetClientId(token);
            var initAccountsBackendRequest = new ClientIdBackendRequest { ClientId = clientId };

            var accountsLiveResponse = await _httpRequestService.RequestAsync<MarginTradingAccountBackendContract[]>(
                initAccountsBackendRequest, "init.accounts");

            var accountsDemoResponse = await _httpRequestService.RequestAsync<MarginTradingAccountBackendContract[]>(
                initAccountsBackendRequest, "init.accounts", false);

            return new InitAccountsLiveDemoClientResponse
            {
                Live = accountsLiveResponse.Select(item => item.ToClientContract()).ToArray(),
                Demo = accountsDemoResponse.Select(item => item.ToClientContract()).ToArray(),
            };
        }

        public async Task<InitAccountInstrumentsLiveDemoClientResponse> AccountInstruments(string token)
        {
            var clientId = await GetClientId(token);
            var initAccountInstrumentsBackendRequest = new ClientIdBackendRequest { ClientId = clientId };

            var instrumentsLiveResponse = await _httpRequestService.RequestAsync<InitAccountInstrumentsBackendResponse>(
                initAccountInstrumentsBackendRequest, "init.accountinstruments");

            var instrumentsDemoResponse = await _httpRequestService.RequestAsync<InitAccountInstrumentsBackendResponse>(
                initAccountInstrumentsBackendRequest, "init.accountinstruments", false);

            return new InitAccountInstrumentsLiveDemoClientResponse
            {
                Live = instrumentsLiveResponse.ToClientContract(),
                Demo = instrumentsDemoResponse.ToClientContract()
            };
        }

        public async Task<InitChartDataClientResponse> InitGraph()
        {
            var initChartDataLiveResponse = await _httpRequestService.RequestAsync<InitChartDataBackendResponse>(null, "init.graph");

            return initChartDataLiveResponse.ToClientContract();
        }

        #endregion

        #region Account

        public async Task<MtClientResponse<bool>> SetActiveAccount(string requestJson)
        {
            var setActiveAccountClientRequest = DeserializeRequest<SetActiveAccountClientRequest>(requestJson);
            var clientId = await GetClientId(setActiveAccountClientRequest.Token);
            var setActiveAccountBackendRequest = setActiveAccountClientRequest.ToBackendContract(clientId);
            var setActiveAccountBackendResponse = await _httpRequestService.RequestAsync<MtBackendResponse<bool>>(setActiveAccountBackendRequest, "account.setActive",
                IsLiveAccount(setActiveAccountBackendRequest.AccountId));
            return setActiveAccountBackendResponse.ToClientContract();
        }

        public async Task<AccountHistoryClientResponse> GetAccountHistory(string requestJson)
        {
            var accountHistoryClientRequest = DeserializeRequest<AccountHistoryClientRequest>(requestJson);
            var clientId = await GetClientId(accountHistoryClientRequest.Token);
            var isLive = !string.IsNullOrEmpty(accountHistoryClientRequest.AccountId)
                ? IsLiveAccount(accountHistoryClientRequest.AccountId)
                : accountHistoryClientRequest.IsLive;
            var accountHistoryBackendRequest = accountHistoryClientRequest.ToBackendContract(clientId);
            var accountHistoryBackendResponse =
                await _httpRequestService.RequestAsync<AccountHistoryBackendResponse>(accountHistoryBackendRequest,
                    "account.history", isLive);
            return accountHistoryBackendResponse.ToClientContract();
        }

        public async Task<AccountHistoryItemClient[]> GetHistory(string requestJson)
        {
            var accountHistoryClientRequest = DeserializeRequest<AccountHistoryClientRequest>(requestJson);
            var clientId = await GetClientId(accountHistoryClientRequest.Token);
            var isLive = !string.IsNullOrEmpty(accountHistoryClientRequest.AccountId)
                ? IsLiveAccount(accountHistoryClientRequest.AccountId)
                : accountHistoryClientRequest.IsLive;
            var accountHistoryBackendRequest = accountHistoryClientRequest.ToBackendContract(clientId);
            var accountHistoryBackendResponse =
                await _httpRequestService.RequestAsync<AccountNewHistoryBackendResponse>(accountHistoryBackendRequest,
                    "account.history.new", isLive);
            return accountHistoryBackendResponse.ToClientContract();
        }

        #endregion

        #region Order

        public async Task<MtClientResponse<OrderClientContract>> PlaceOrder(string requestJson)
        {
            var clientRequest = DeserializeRequest<OpenOrderClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);
            var backendRequest = clientRequest.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<OpenOrderBackendResponse>(backendRequest, "order.place",
                IsLiveAccount(backendRequest.Order.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<MtClientResponse<bool>> CloseOrder(string requestJson)
        {
            var clientRequest = DeserializeRequest<CloseOrderClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);
            var backendRequest = clientRequest.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<MtBackendResponse<bool>>(backendRequest, "order.close",
                IsLiveAccount(backendRequest.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<MtClientResponse<bool>> CancelOrder(string requestJson)
        {
            var clientRequest = DeserializeRequest<CloseOrderClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);
            var backendRequest = clientRequest.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<MtBackendResponse<bool>>(backendRequest, "order.cancel",
                IsLiveAccount(backendRequest.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<ClientOrdersLiveDemoClientResponse> GetOpenPositions(string token)
        {
            var clientId = await GetClientId(token);
            var backendRequest = new ClientIdBackendRequest { ClientId = clientId };
            var backendLiveResponse = await _httpRequestService.RequestAsync<OrderBackendContract[]>(backendRequest, "order.list");
            var backendDemoResponse = await _httpRequestService.RequestAsync<OrderBackendContract[]>(backendRequest, "order.list", false);

            return new ClientOrdersLiveDemoClientResponse
            {
                Live = backendLiveResponse.Select(item => item.ToClientContract()).ToArray(),
                Demo = backendDemoResponse.Select(item => item.ToClientContract()).ToArray()
            };
        }

        public async Task<OrderClientContract[]> GetAccountOpenPositions(string requestJson)
        {
            var clientRequest = DeserializeRequest<AccountTokenClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);
            var backendRequest = clientRequest.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<OrderBackendContract[]>(backendRequest, "order.account.list", IsLiveAccount(backendRequest.AccountId));

            return backendResponse.Select(item => item.ToClientContract()).ToArray();
        }

        public async Task<ClientPositionsLiveDemoClientResponse> GetClientOrders(string token)
        {
            var clientId = await GetClientId(token);
            var backendRequest = new ClientIdBackendRequest { ClientId = clientId };
            var backendLiveResponse = await _httpRequestService.RequestAsync<ClientOrdersBackendResponse>(backendRequest, "order.positions");
            var backendDemoResponse = await _httpRequestService.RequestAsync<ClientOrdersBackendResponse>(backendRequest, "order.positions", false);

            return new ClientPositionsLiveDemoClientResponse
            {
                Live = backendLiveResponse.ToClientContract(),
                Demo = backendDemoResponse.ToClientContract()
            };
        }

        public async Task<MtClientResponse<bool>> ChangeOrderLimits(string requestJson)
        {
            var clientRequest = DeserializeRequest<ChangeOrderLimitsClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);
            var backendRequest = clientRequest.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<MtBackendResponse<bool>>(backendRequest, "order.changeLimits",
                IsLiveAccount(backendRequest.AccountId));
            return backendResponse.ToClientContract();
        } 

        #endregion

        #region Orderbook

        public async Task<Dictionary<string, OrderBookClientContract>> GetOrderBooks()
        {
            var backendResponse = await _httpRequestService.RequestAsync<OrderbooksBackendResponse>(null, "orderbooks");
            return backendResponse.ToClientContract();
        }

        #endregion

        #region Private methods

        private TRequestContract DeserializeRequest<TRequestContract>(string requestJson)
        {
            if (string.IsNullOrWhiteSpace(requestJson))
                throw new ArgumentNullException(nameof(requestJson));

            var result = JsonConvert.DeserializeObject<TRequestContract>(requestJson);

            var validationContext = new ValidationContext(result);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(result, validationContext, validationResults, true))
            {
                var errorMessage =
                    validationResults.Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))
                        .Select(x => x.ErrorMessage)
                        .Aggregate((x, y) => x + "; " + y);
                errorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? $"Request {requestJson} contains validation errors" :
                    $"Request {requestJson} contains validation errors: {errorMessage}";
                throw new ValidationException(errorMessage);
            }

            return result;
        }

        private async Task<string> GetClientId(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new Exception("Token is null or empty");

            string clientId = await _clientTokenService.GetClientId(token);

            if (string.IsNullOrWhiteSpace(clientId))
                throw new KeyNotFoundException($"Can't find session by provided token '{token}'");

            return clientId;
        }

        private bool IsLiveAccount(string accountId)
        {
            return !accountId.StartsWith(_settings.MarginTradingFront.DemoAccountIdPrefix);
        }

        #endregion
    }
}
