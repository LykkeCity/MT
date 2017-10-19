using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Common.ClientContracts;
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
        private readonly RpcFacade _rpcFacade;
        private readonly IDateService _dateService;

        public RpcMtFrontend(
            MtFrontendSettings settings,
            IClientTokenService clientTokenService,
            RpcFacade rpcFacade,
            IDateService dateService)
        {
            _settings = settings;
            _clientTokenService = clientTokenService;
            _rpcFacade = rpcFacade;
            _dateService = dateService;
        }

        #region Service

        public IsAliveResponse IsAlive()
        {
            return new IsAliveResponse
            {
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = _settings.MarginTradingFront.Env,
                ServerTime = _dateService.Now()
            };
        }

        #endregion


        #region Init data

        public async Task<InitDataLiveDemoClientResponse> InitData(string token)
        {
            var clientId = await GetClientId(token);

            return await _rpcFacade.InitData(clientId);
        }

        public async Task<InitAccountsLiveDemoClientResponse> InitAccounts(string token)
        {
            var clientId = await GetClientId(token);

            return await _rpcFacade.InitAccounts(clientId);
        }

        public async Task<InitAccountInstrumentsLiveDemoClientResponse> AccountInstruments(string token)
        {
            var clientId = await GetClientId(token);

            return await _rpcFacade.AccountInstruments(clientId);
        }

        public async Task<InitChartDataClientResponse> InitGraph(string token = null, string[] assetIds = null)
        {
            var clientId = string.IsNullOrEmpty(token) ? null : await GetClientId(token);

            return await _rpcFacade.InitGraph(clientId, assetIds);
        }

        #endregion


        #region Account

        public async Task<AccountHistoryClientResponse> GetAccountHistory(string requestJson)
        {
            var accountHistoryClientRequest = DeserializeRequest<AccountHistoryRpcClientRequest>(requestJson);
            var clientId = await GetClientId(accountHistoryClientRequest.Token);

            return await _rpcFacade.GetAccountHistory(clientId, accountHistoryClientRequest);
        }

        public async Task<AccountHistoryItemClient[]> GetHistory(string requestJson)
        {
            var accountHistoryClientRequest = DeserializeRequest<AccountHistoryRpcClientRequest>(requestJson);
            var clientId = await GetClientId(accountHistoryClientRequest.Token);

            return await _rpcFacade.GetAccountHistoryTimeline(clientId, accountHistoryClientRequest);
        }

        #endregion


        #region Order

        public async Task<MtClientResponse<OrderClientContract>> PlaceOrder(string requestJson)
        {
            var clientRequest = DeserializeRequest<OpenOrderRpcClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);

            return await _rpcFacade.PlaceOrder(clientId, clientRequest.Order);
        }

        public async Task<MtClientResponse<bool>> CloseOrder(string requestJson)
        {
            var clientRequest = DeserializeRequest<CloseOrderRpcClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);

            return await _rpcFacade.CloseOrder(clientId, clientRequest);
        }

        public async Task<MtClientResponse<bool>> CancelOrder(string requestJson)
        {
            var clientRequest = DeserializeRequest<CloseOrderRpcClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);

            return await _rpcFacade.CancelOrder(clientId, clientRequest);
        }

        public async Task<ClientOrdersLiveDemoClientResponse> GetOpenPositions(string token)
        {
            var clientId = await GetClientId(token);

            return await _rpcFacade.GetOpenPositions(clientId);
        }

        public async Task<OrderClientContract[]> GetAccountOpenPositions(string requestJson)
        {
            var clientRequest = DeserializeRequest<AccountTokenClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);

            return await _rpcFacade.GetAccountOpenPositions(clientId, clientRequest.AccountId);
        }

        public async Task<ClientPositionsLiveDemoClientResponse> GetClientOrders(string token)
        {
            var clientId = await GetClientId(token);

            return await _rpcFacade.GetClientOrders(clientId);
        }

        public async Task<MtClientResponse<bool>> ChangeOrderLimits(string requestJson)
        {
            var clientRequest = DeserializeRequest<ChangeOrderLimitsRpcClientRequest>(requestJson);
            var clientId = await GetClientId(clientRequest.Token);

            return await _rpcFacade.ChangeOrderLimits(clientId, clientRequest);
        } 

        #endregion


        #region Orderbook

        public Task<OrderBookClientContract> GetOrderBook(string instrument)
        {
            return _rpcFacade.GetOrderBook(instrument);
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

        #endregion
    }
}
