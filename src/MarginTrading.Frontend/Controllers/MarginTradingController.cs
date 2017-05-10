using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using MarginTrading.Core;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Settings;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    //TODO: временное решение!
    [Route("api/margintrading")]
    public class MarginTradingController : Controller
    {
        private readonly IMarginTradingAccountsRepository _accountsRepository;
        private readonly IMarginTradingConditionRepository _tradingConditionRepository;
        private readonly IClientTokenService _clientTokenService;
        private readonly MtFrontendSettings _settings;

        public MarginTradingController(
            IMarginTradingAccountsRepository accountsRepository,
            IMarginTradingConditionRepository tradingConditionRepository,
            IClientTokenService clientTokenService,
            MtFrontendSettings settings
            )
        {
            _accountsRepository = accountsRepository;
            _tradingConditionRepository = tradingConditionRepository;
            _clientTokenService = clientTokenService;
            _settings = settings;
        }

        [HttpGet]
        [Route("status/{token}")]
        [ProducesResponseType(typeof(ResponseModel<MarginTradingStatus>), 200)]
        public async Task<ResponseModel<MarginTradingStatus>> GetMarginTradingStatus(string token)
        {
            var clientId = await _clientTokenService.GetClientId(token);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return ResponseModel<MarginTradingStatus>
                    .CreateFail(ResponseModel.ErrorCodeType.NoData, $"Can't find session by provided token '{token}'");
            }

            var accounts = await _accountsRepository.GetAllAsync(clientId);

            return accounts.Any()
                ? ResponseModel<MarginTradingStatus>.CreateOk(new MarginTradingStatus { Status = MtStatus.Available})
                : ResponseModel<MarginTradingStatus>.CreateOk(new MarginTradingStatus { Status = MtStatus.AgreeTerms, Url = "http://google.com"});
        }

        [HttpPost]
        [Route("agree")]
        [ProducesResponseType(typeof(ResponseModel<MtStatus>), 200)]
        public async Task<ResponseModel<MtStatus>> CreateMarginTradingAccounts([FromBody]TokenModel model)
        {
            var clientId = await _clientTokenService.GetClientId(model.Token);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return ResponseModel<MtStatus>
                    .CreateFail(ResponseModel.ErrorCodeType.NoData, $"Can't find session by provided token '{model.Token}'");
            }

            var accounts = await _accountsRepository.GetAllAsync(clientId);

            if (accounts.Any())
            {
                return ResponseModel<MtStatus>.CreateOk(MtStatus.Available);
            }

            return await CreateMockAccounts(clientId);
        }

        private async Task<ResponseModel<MtStatus>> CreateMockAccounts(string clientId)
        {
            var tradingConditions = await _tradingConditionRepository.GetAllAsync();
            string tradingConditionId = tradingConditions.FirstOrDefault(item => item.IsDefault)?.Id ?? string.Empty;

            if (string.IsNullOrEmpty(tradingConditionId))
            {
                return ResponseModel<MtStatus>
                    .CreateFail(ResponseModel.ErrorCodeType.NoData, "Can't create accounts - no default trading condition");
            }

            var accounts = new List<MarginTradingAccount>
            {
                new MarginTradingAccount
                {
                    Id = Guid.NewGuid().ToString("N"),
                    BaseAssetId = "EUR",
                    ClientId = clientId,
                    Balance = 50000,
                    IsCurrent = true,
                    TradingConditionId = tradingConditionId
                },
                new MarginTradingAccount
                {
                    Id = Guid.NewGuid().ToString("N"),
                    BaseAssetId = "USD",
                    ClientId = clientId,
                    Balance = 50000,
                    IsCurrent = false,
                    TradingConditionId = tradingConditionId
                },
                new MarginTradingAccount
                {
                    Id = Guid.NewGuid().ToString("N"),
                    BaseAssetId = "CHF",
                    ClientId = clientId,
                    Balance = 50000,
                    IsCurrent = false,
                    TradingConditionId = tradingConditionId
                }
            };

            foreach (var account in accounts)
            {
                await _accountsRepository.AddAsync(account);
            }

            await UpdateAccountsCache(clientId);

            return ResponseModel<MtStatus>.CreateOk(MtStatus.Created);
        }

        private async Task UpdateAccountsCache(string clientId)
        {
            var url = $"{_settings.MarginTradingDemo.ApiRootUrl}/api/backoffice/updateAccounts";
            await url
                    .SetQueryParams(new { clientId = clientId})
                    .WithHeader("api-key", _settings.MarginTradingDemo.ApiKey)
                    .PostAsync(null);
        }
    }

    public class MarginTradingStatus
    {
        public MtStatus Status { get; set; }
        public string Url { get; set; }
    }

    public enum MtStatus
    {
        Available,
        AgreeTerms,
        Created
    }
}
