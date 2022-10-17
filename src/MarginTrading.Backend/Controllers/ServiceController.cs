// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/service")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class ServiceController : Controller, IServiceApi
    {
        private readonly IOvernightMarginParameterContainer _overnightMarginParameterContainer;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ISnapshotService _snapshotService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IAccountsProvider _accountsProvider;

        public ServiceController(
            IOvernightMarginParameterContainer overnightMarginParameterContainer,
            IIdentityGenerator identityGenerator,
            ISnapshotService snapshotService,
            IAccountUpdateService accountUpdateService,
            IAccountsProvider accountsProvider)
        {
            _overnightMarginParameterContainer = overnightMarginParameterContainer;
            _identityGenerator = identityGenerator;
            _snapshotService = snapshotService;
            _accountUpdateService = accountUpdateService;
            _accountsProvider = accountsProvider;
        }

        /// <summary>
        /// Save snapshot of orders, positions, account stats, best fx prices, best trading prices for current moment.
        /// Throws an error in case if trading is not stopped.
        /// </summary>
        /// <returns>Snapshot statistics.</returns>
        [HttpPost("make-trading-data-snapshot")]
        public async Task<string> MakeTradingDataSnapshot([FromQuery] DateTime tradingDay, [FromQuery] string correlationId = null)
        {
            if (tradingDay == default)
            {
                throw new Exception($"{nameof(tradingDay)} must be set");
            }
            
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = _identityGenerator.GenerateGuid();
            }
            
            return await _snapshotService.MakeTradingDataSnapshot(tradingDay, correlationId);
        }

        /// <summary>
        /// Get current state of overnight margin parameter.
        /// </summary>
        [HttpGet("current-overnight-margin-parameter")]
        public Task<bool> GetOvernightMarginParameterCurrentState()
        {
            return Task.FromResult(_overnightMarginParameterContainer.GetOvernightMarginParameterState());
        }

        /// <summary>
        /// Get current margin parameter values for instruments (all / filtered by IDs).
        /// </summary>
        /// <returns>
        /// Dictionary with key = asset pair ID and value = (Dictionary with key = trading condition ID and value = multiplier)
        /// </returns>
        [HttpGet("overnight-margin-parameter")]
        public Task<Dictionary<string, Dictionary<string, decimal>>> GetOvernightMarginParameterValues(
            [FromQuery] string[] instruments = null)
        {
            var result = _overnightMarginParameterContainer.GetOvernightMarginParameterValues()
                .Where(x => instruments == null || !instruments.Any() || instruments.Contains(x.Key.Item2))
                .GroupBy(x => x.Key.Item2)
                .OrderBy(x => x.Any(p => p.Value > 1) ? 0 : 1)
                .ThenBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.ToDictionary(p => p.Key.Item1, p => p.Value));
            
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        [HttpGet("unconfirmed-margin")]
        public Dictionary<string, decimal> GetUnconfirmedMargin([FromQuery] string accountId)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account == null)
                return new Dictionary<string, decimal>();

            return account
                .AccountFpl
                .UnconfirmedMarginData
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <inheritdoc />
        [HttpPost("unconfirmed-margin")]
        public Task UnfreezeUnconfirmedMargin([FromQuery] string accountId, [FromQuery] string operationId)
        {
            return _accountUpdateService.UnfreezeUnconfirmedMargin(accountId, operationId);
        }

        /// <inheritdoc />
        [HttpPut("unconfirmed-margin")]
        public Task FreezeUnconfirmedMargin(string accountId, string operationId, decimal amount)
        {
            return _accountUpdateService.FreezeUnconfirmedMargin(accountId, operationId, amount);
        }
    }
}