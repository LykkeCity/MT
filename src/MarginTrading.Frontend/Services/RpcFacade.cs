using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;
using MarginTrading.Frontend.Settings;

namespace MarginTrading.Frontend.Services
{
    public class RpcFacade
    {
        private readonly MtFrontendSettings _settings;
        private readonly HttpRequestService _httpRequestService;
        private readonly IMarginTradingSettingsService _marginTradingSettingsService;

        public RpcFacade(
            MtFrontendSettings settings,
            HttpRequestService httpRequestService,
            IMarginTradingSettingsService marginTradingSettingsService)
        {
            _settings = settings;
            _httpRequestService = httpRequestService;
            _marginTradingSettingsService = marginTradingSettingsService;
        }

        #region Init data

        public async Task<InitDataLiveDemoClientResponse> InitData(string clientId)
        {
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

            initData.Assets = assetsLive.Concat(assetsDemo).GroupBy(a => a.Id)
                .Select(g => g.First().ToClientContract()).ToArray();

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

        public async Task<InitAccountsLiveDemoClientResponse> InitAccounts(string clientId)
        {
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

        public async Task<InitAccountInstrumentsLiveDemoClientResponse> AccountInstruments(string clientId)
        {
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

        public async Task<InitChartDataClientResponse> InitGraph(string clientId = null)
        {
            var request = new ClientIdBackendRequest { ClientId = clientId };

            var initChartDataLiveResponse = await _httpRequestService.RequestAsync<InitChartDataBackendResponse>(request, "init.graph");

            return initChartDataLiveResponse.ToClientContract();
        }

        public async Task<Dictionary<string, BidAskClientContract>> InitPrices(string clientId = null, string[] assetIds = null)
        {
            var request = new InitPricesBackendRequest {ClientId = clientId, AssetIds = assetIds};

            var initPricesResponse = await _httpRequestService
                .RequestAsync<Dictionary<string, InstrumentBidAskPairContract>>(request, "init.prices");

            return initPricesResponse.ToDictionary(p => p.Key, p => p.Value.ToClientContract());
        }

        #endregion


        #region Account

        public async Task<AccountHistoryClientResponse> GetAccountHistory(string clientId, AccountHistoryFiltersClientRequest request)
        {
            var isLive = !string.IsNullOrEmpty(request.AccountId)
                ? IsLiveAccount(request.AccountId)
                : request.IsLive;
            var accountHistoryBackendRequest = request.ToBackendContract(clientId);
            var accountHistoryBackendResponse =
                await _httpRequestService.RequestAsync<AccountHistoryBackendResponse>(accountHistoryBackendRequest,
                    "account.history", isLive);
            return accountHistoryBackendResponse.ToClientContract();
        }

        public async Task<AccountHistoryItemClient[]> GetAccountHistoryTimeline(string clientId, AccountHistoryFiltersClientRequest request)
        {
            var isLive = !string.IsNullOrEmpty(request.AccountId)
                ? IsLiveAccount(request.AccountId)
                : request.IsLive;
            var accountHistoryBackendRequest = request.ToBackendContract(clientId);
            var accountHistoryBackendResponse =
                await _httpRequestService.RequestAsync<AccountNewHistoryBackendResponse>(accountHistoryBackendRequest,
                    "account.history.new", isLive);
            return accountHistoryBackendResponse.ToClientContract();
        }

        #endregion


        #region Order

        public async Task<MtClientResponse<OrderClientContract>> PlaceOrder(string clientId, NewOrderClientContract request)
        {
            var backendRequest = request.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<OpenOrderBackendResponse>(backendRequest, "order.place",
                IsLiveAccount(backendRequest.Order.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<MtClientResponse<bool>> CloseOrder(string clientId, CloseOrderClientRequest request)
        {
            var backendRequest = request.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<MtBackendResponse<bool>>(backendRequest, "order.close",
                IsLiveAccount(backendRequest.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<MtClientResponse<bool>> CancelOrder(string clientId, CloseOrderClientRequest request)
        {
            var backendRequest = request.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<MtBackendResponse<bool>>(backendRequest, "order.cancel",
                IsLiveAccount(backendRequest.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<ClientOrdersLiveDemoClientResponse> GetOpenPositions(string clientId)
        {
            var backendRequest = new ClientIdBackendRequest { ClientId = clientId };
            var backendLiveResponse = await _httpRequestService.RequestAsync<OrderBackendContract[]>(backendRequest, "order.list");
            var backendDemoResponse = await _httpRequestService.RequestAsync<OrderBackendContract[]>(backendRequest, "order.list", false);

            return new ClientOrdersLiveDemoClientResponse
            {
                Live = backendLiveResponse.Select(item => item.ToClientContract()).ToArray(),
                Demo = backendDemoResponse.Select(item => item.ToClientContract()).ToArray()
            };
        }

        public async Task<OrderClientContract[]> GetAccountOpenPositions(string clientId, string accountId)
        {
            var backendRequest = new AccountClientIdBackendRequest {AccountId = accountId, ClientId = clientId};
            var backendResponse = await _httpRequestService.RequestAsync<OrderBackendContract[]>(backendRequest,
                "order.account.list", IsLiveAccount(backendRequest.AccountId));

            return backendResponse.Select(item => item.ToClientContract()).ToArray();
        }

        public async Task<ClientPositionsLiveDemoClientResponse> GetClientOrders(string clientId)
        {
            var backendRequest = new ClientIdBackendRequest { ClientId = clientId };
            var backendLiveResponse = await _httpRequestService.RequestAsync<ClientOrdersBackendResponse>(backendRequest, "order.positions");
            var backendDemoResponse = await _httpRequestService.RequestAsync<ClientOrdersBackendResponse>(backendRequest, "order.positions", false);

            return new ClientPositionsLiveDemoClientResponse
            {
                Live = backendLiveResponse.ToClientContract(),
                Demo = backendDemoResponse.ToClientContract()
            };
        }

        public async Task<MtClientResponse<bool>> ChangeOrderLimits(string clientId,
            ChangeOrderLimitsClientRequest request)
        {
            var backendRequest = request.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestAsync<MtBackendResponse<bool>>(backendRequest,
                "order.changeLimits",
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

        private bool IsLiveAccount(string accountId)
        {
            return !accountId.StartsWith(_settings.MarginTradingFront.DemoAccountIdPrefix);
        }

        #endregion
    }
}
