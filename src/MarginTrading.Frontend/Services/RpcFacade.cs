﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MarginTrading.Backend.Contracts.DataReaderClient;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Contract.ClientContracts;
using MarginTrading.Contract.Mappers;
using MarginTrading.Frontend.Settings;

namespace MarginTrading.Frontend.Services
{
    public class RpcFacade
    {
        private readonly MtFrontendSettings _settings;
        private readonly IHttpRequestService _httpRequestService;
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;
        private readonly IMtDataReaderClientsPair _dataReaderClients;

        public RpcFacade(
            MtFrontendSettings settings,
            IHttpRequestService httpRequestService,
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService,
            IMtDataReaderClientsPair dataReaderClients)
        {
            _settings = settings;
            _httpRequestService = httpRequestService;
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
            _dataReaderClients = dataReaderClients;
        }

        #region Init data

        public async Task<InitDataLiveDemoClientResponse> InitData(string clientId)
        {
            var marginTradingEnabled = await _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId);
            if (!marginTradingEnabled.Demo && !marginTradingEnabled.Live)
            {
                throw new Exception("Margin trading is not available");
            }

            var initData = new InitDataLiveDemoClientResponse();
            var clientIdRequest = new ClientIdBackendRequest {ClientId = clientId};
            
            var initAssetsResponses = await _httpRequestService.RequestIfAvailableAsync(clientIdRequest, "init.assets", Array.Empty<AssetPairBackendContract>, marginTradingEnabled);
            initData.Assets = initAssetsResponses.Live.Concat(initAssetsResponses.Demo).GroupBy(a => a.Id)
                .Select(g => g.First().ToClientContract()).ToArray();

            var initPricesResponse =
                await _httpRequestService.RequestWithRetriesAsync<Dictionary<string, InstrumentBidAskPairContract>>(
                    clientIdRequest, "init.prices");

            initData.Prices = initPricesResponse.ToDictionary(p => p.Key, p => p.Value.ToClientContract());

            var initDataResponses = await _httpRequestService.RequestIfAvailableAsync<InitDataBackendResponse>(clientIdRequest, "init.data", () => null, marginTradingEnabled);
            initData.Live = initDataResponses.Live?.ToClientContract();
            initData.Demo = initDataResponses.Demo?.ToClientContract();

            return initData;
        }

        public async Task<InitAccountsLiveDemoClientResponse> InitAccounts(string clientId)
        {
            var marginTradingEnabled = await _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId);
            var responses = await _httpRequestService.RequestIfAvailableAsync(new ClientIdBackendRequest { ClientId = clientId },
                                                                              "init.accounts",
                                                                              Array.Empty<MarginTradingAccountBackendContract>,
                                                                              marginTradingEnabled);
            return new InitAccountsLiveDemoClientResponse
            {
                Live = responses.Live.Select(item => item.ToClientContract()).ToArray(),
                Demo = responses.Demo.Select(item => item.ToClientContract()).ToArray(),
            };
        }

        public async Task<InitAccountInstrumentsLiveDemoClientResponse> AccountInstruments(string clientId)
        {
            var marginTradingEnabled = await _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId);
            var responses = await _httpRequestService.RequestIfAvailableAsync(new ClientIdBackendRequest { ClientId = clientId },
                                                                              "init.accountinstruments",
                                                                              InitAccountInstrumentsBackendResponse.CreateEmpty,
                                                                              marginTradingEnabled);

            return new InitAccountInstrumentsLiveDemoClientResponse
            {
                Live = responses.Live.ToClientContract(),
                Demo = responses.Demo.ToClientContract()
            };
        }

        public async Task<InitChartDataClientResponse> InitGraph(string clientId = null, string[] assetIds = null)
        {
            var request = new InitChartDataBackendRequest {ClientId = clientId, AssetIds = assetIds};

            var initChartDataLiveResponse = await _httpRequestService.RequestWithRetriesAsync<InitChartDataBackendResponse>(request, "init.graph");

            return initChartDataLiveResponse.ToClientContract();
        }

        public async Task<Dictionary<string, BidAskClientContract>> InitPrices(string clientId = null, string[] assetIds = null)
        {
            var request = new InitPricesBackendRequest {ClientId = clientId, AssetIds = assetIds};

            var initPricesResponse = await _httpRequestService
                .RequestWithRetriesAsync<Dictionary<string, InstrumentBidAskPairContract>>(request, "init.prices");

            return initPricesResponse.ToDictionary(p => p.Key, p => p.Value.ToClientContract());
        }

        #endregion


        #region Account

        public async Task<AccountHistoryClientResponse> GetAccountHistory(string clientId, AccountHistoryFiltersClientRequest request)
        {
            var isLive = !string.IsNullOrEmpty(request.AccountId)
                ? IsLiveAccount(request.AccountId)
                : request.IsLive;

            var marginTradingEnabled = await _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId, isLive);
            if (!marginTradingEnabled)
            {
                return new AccountHistoryClientResponse
                {
                    Account = Array.Empty<AccountHistoryClientContract>(),
                    OpenPositions = Array.Empty<OrderHistoryClientContract>(),
                    PositionsHistory = Array.Empty<OrderHistoryClientContract>(),
                };
            }

            var accountHistoryBackendResponse = await _dataReaderClients.Get(isLive).AccountHistory.ByTypes(
                new AccountHistoryBackendRequest
                {
                    AccountId = request.AccountId,
                    ClientId = clientId,
                    From = request.From,
                    To = request.To
                });
            return ToClientContract(accountHistoryBackendResponse);
        }

        public async Task<AccountHistoryItemClient[]> GetAccountHistoryTimeline(string clientId, AccountHistoryFiltersClientRequest request)
        {
            var isLive = !string.IsNullOrEmpty(request.AccountId)
                ? IsLiveAccount(request.AccountId)
                : request.IsLive;

            var marginTradingEnabled = await _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId, isLive);
            if (!marginTradingEnabled)
            {
                return Array.Empty<AccountHistoryItemClient>();
            }
            var accountHistoryBackendResponse = await _dataReaderClients.Get(isLive).AccountHistory.Timeline(new AccountHistoryBackendRequest
            {
                AccountId = request.AccountId,
                ClientId = clientId,
                From = request.From,
                To = request.To
            });
            return ToClientContract(accountHistoryBackendResponse);
        }

        #endregion


        #region Order

        public async Task<MtClientResponse<OrderClientContract>> PlaceOrder(string clientId, NewOrderClientContract request)
        {
            var backendRequest = request.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestWithRetriesAsync<OpenOrderBackendResponse>(backendRequest, "order.place",
                IsLiveAccount(backendRequest.Order.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<MtClientResponse<bool>> CloseOrder(string clientId, CloseOrderClientRequest request)
        {
            var backendRequest = request.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestWithRetriesAsync<MtBackendResponse<bool>>(backendRequest, "order.close",
                IsLiveAccount(backendRequest.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<MtClientResponse<bool>> CancelOrder(string clientId, CloseOrderClientRequest request)
        {
            var backendRequest = request.ToBackendContract(clientId);
            var backendResponse = await _httpRequestService.RequestWithRetriesAsync<MtBackendResponse<bool>>(backendRequest, "order.cancel",
                IsLiveAccount(backendRequest.AccountId));
            return backendResponse.ToClientContract();
        }

        public async Task<ClientOrdersLiveDemoClientResponse> GetOpenPositions(string clientId)
        {
            var marginTradingEnabled = await _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId);
            var responses = await _httpRequestService.RequestIfAvailableAsync(new ClientIdBackendRequest { ClientId = clientId },
                                                                              "order.list",
                                                                              Array.Empty<OrderBackendContract>,
                                                                              marginTradingEnabled);

            return new ClientOrdersLiveDemoClientResponse
            {
                Live = responses.Live.Select(item => item.ToClientContract()).ToArray(),
                Demo = responses.Demo.Select(item => item.ToClientContract()).ToArray()
            };
        }

        public async Task<OrderClientContract[]> GetAccountOpenPositions(string clientId, string accountId)
        {
            var backendRequest = new AccountClientIdBackendRequest {AccountId = accountId, ClientId = clientId};
            var backendResponse = await _httpRequestService.RequestWithRetriesAsync<OrderBackendContract[]>(backendRequest,
                "order.account.list", IsLiveAccount(backendRequest.AccountId));

            return backendResponse.Select(item => item.ToClientContract()).ToArray();
        }

        public async Task<ClientPositionsLiveDemoClientResponse> GetClientOrders(string clientId)
        {
            var marginTradingEnabled = await _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId);
            var responses = await _httpRequestService.RequestIfAvailableAsync(new ClientIdBackendRequest { ClientId = clientId },
                                                                              "order.positions",
                                                                              () => new ClientOrdersBackendResponse
                                                                              {
                                                                                  Orders = Array.Empty<OrderBackendContract>(),
                                                                                  Positions = Array.Empty<OrderBackendContract>()
                                                                              },
                                                                              marginTradingEnabled);

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
            var backendResponse = await _httpRequestService.RequestWithRetriesAsync<MtBackendResponse<bool>>(backendRequest,
                "order.changeLimits",
                IsLiveAccount(backendRequest.AccountId));
            return backendResponse.ToClientContract();
        }

        #endregion


        #region Orderbook

        public async Task<OrderBookClientContract> GetOrderBook(string instrument)
        {
            var backendResponse =
                await _httpRequestService.RequestWithRetriesAsync<OrderbooksBackendResponse>(
                    new OrderbooksBackendRequest {Instrument = instrument}, "orderbooks");
            return backendResponse.Orderbook.ToClientContract();
        }

        #endregion


        #region Private methods

        private bool IsLiveAccount(string accountId)
        {
            return !accountId.StartsWith(_settings.MarginTradingFront.DemoAccountIdPrefix);
        }

        private static AccountHistoryClientResponse ToClientContract(AccountHistoryBackendResponse src)
        {
            return new AccountHistoryClientResponse
            {
                Account = src.Account.Select(ToClientContract).OrderByDescending(item => item.Date).ToArray(),
                OpenPositions = src.OpenPositions.Select(ToClientContract).ToArray(),
                PositionsHistory = src.PositionsHistory.Select(ToClientContract).ToArray()
            };
        }

        private static AccountHistoryClientContract ToClientContract(AccountHistoryBackendContract src)
        {
            return new AccountHistoryClientContract
            {
                Id = src.Id,
                Date = src.Date,
                AccountId = src.AccountId,
                ClientId = src.ClientId,
                Amount = src.Amount,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                Comment = src.Comment,
                Type = ConvertEnum<AccountHistoryTypeContract>(src.Type),
                LegalEnity = src.LegalEntity,
            };
        }

        private static OrderHistoryClientContract ToClientContract(OrderHistoryBackendContract src)
        {
            return new OrderHistoryClientContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                AssetAccuracy = src.AssetAccuracy,
                Type = ConvertEnum<OrderDirectionContract>(src.Type),
                Status = ConvertEnum<OrderStatusContract>(src.Status),
                CloseReason = ConvertEnum<OrderCloseReasonContract>(src.CloseReason),
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                TotalPnL = src.TotalPnl,
                PnL = src.Pnl,
                InterestRateSwap = src.InterestRateSwap,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission
            };
        }

        public static AccountHistoryItemClient[] ToClientContract(AccountNewHistoryBackendResponse src)
        {
            return src.HistoryItems.Select(i => new AccountHistoryItemClient
            {
                Date = i.Date,
                Account = i.Account == null ? null : ToClientContract(i.Account),
                Position = i.Position == null ? null : ToClientContract(i.Position)
            }).ToArray();
        }

        private static TResult ConvertEnum<TResult>(Enum e)
        {
            return e.ToString().ParseEnum<TResult>();
        }

        #endregion
    }
}
