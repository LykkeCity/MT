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
            var marginTradingDemoEnabled = await _marginTradingSettingsService.IsMarginTradingDemoEnabled(clientId);
            var marginTradingLiveEnabled = await _marginTradingSettingsService.IsMarginTradingLiveEnabled(clientId);

            if (!marginTradingDemoEnabled && !marginTradingLiveEnabled)
            {
                throw new Exception("Margin trading is not available");
            }

            var initDataBackendRequest = new ClientIdBackendRequest { ClientId = clientId };

            var initData = new InitDataLiveDemoClientResponse();

            var assets = await RequestAvailableBackendTypes(clientId,
                                                               null,
                                                               "init.assets",
                                                               Array.Empty<MarginTradingAssetBackendContract>);

            initData.Assets = assets.Live.Concat(assets.Demo).GroupBy(a => a.Id)
                .Select(g => g.First().ToClientContract()).ToArray();

            var initPricesResponse = await _httpRequestService
                .RequestAsync<Dictionary<string, InstrumentBidAskPairContract>>(
                    new InitPricesBackendRequest {ClientId = clientId}, "init.prices");

            initData.Prices = initPricesResponse.ToDictionary(p => p.Key, p => p.Value.ToClientContract());

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
            var responses = await RequestAvailableBackendTypes(clientId,
                                                               new ClientIdBackendRequest { ClientId = clientId },
                                                               "init.accounts",
                                                               Array.Empty<MarginTradingAccountBackendContract>);
            return new InitAccountsLiveDemoClientResponse
            {
                Live = responses.Live.Select(item => item.ToClientContract()).ToArray(),
                Demo = responses.Demo.Select(item => item.ToClientContract()).ToArray(),
            };
        }

        public async Task<InitAccountInstrumentsLiveDemoClientResponse> AccountInstruments(string clientId)
        {
            var responses = await RequestAvailableBackendTypes(clientId,
                                                               new ClientIdBackendRequest { ClientId = clientId },
                                                               "init.accountinstruments",
                                                               InitAccountInstrumentsBackendResponse.CreateEmpty);

            return new InitAccountInstrumentsLiveDemoClientResponse
            {
                Live = responses.Live.ToClientContract(),
                Demo = responses.Demo.ToClientContract()
            };
        }

        public async Task<InitChartDataClientResponse> InitGraph(string clientId = null, string[] assetIds = null)
        {
            var request = new InitChartDataBackendRequest {ClientId = clientId, AssetIds = assetIds};

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
            var responses = await RequestAvailableBackendTypes(clientId,
                                                               new ClientIdBackendRequest { ClientId = clientId },
                                                               "order.list",
                                                               Array.Empty<OrderBackendContract>);

            return new ClientOrdersLiveDemoClientResponse
            {
                Live = responses.Live.Select(item => item.ToClientContract()).ToArray(),
                Demo = responses.Demo.Select(item => item.ToClientContract()).ToArray()
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
            var responses = await RequestAvailableBackendTypes(clientId,
                                                               new ClientIdBackendRequest { ClientId = clientId },
                                                               "order.positions",
                                                               () => new ClientOrdersBackendResponse { Orders = Array.Empty<OrderBackendContract>(), Positions = Array.Empty<OrderBackendContract>() });

            return new ClientPositionsLiveDemoClientResponse
            {
                Live = responses.Live.ToClientContract(),
                Demo = responses.Demo.ToClientContract()
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

        /// <summary>
        /// Get responses from backends available for <paramref name="clientId"/> (live/demo).
        /// If a backend is not available - <paramref name="defaultResult"/> is returned instead.
        /// </summary>
        private async Task<(TResponse Demo, TResponse Live)> RequestAvailableBackendTypes<TResponse>(string clientId, object request, string action, Func<TResponse> defaultResult, string controller = "mt")
            where TResponse: class
        {
            async Task<TResponse> Request(bool isLive) => await _marginTradingSettingsService.IsMarginTradingEnabled(clientId, isLive)
                                                              ? await _httpRequestService.RequestAsync<TResponse>(request, action, isLive, controller)
                                                              : defaultResult();

            // todo if task failed return default value instead
            var tasks = await Task.WhenAll(Request(false), Request(true));
            return (Demo: tasks[0], Live: tasks[1]);
        }

        #endregion
    }
}
