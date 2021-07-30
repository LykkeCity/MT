// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Testing;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/testing")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class TestingController : Controller, ITestingApi
    {
        private readonly IFakeSnapshotService _fakeSnapshotService;
        private readonly string _protectionKey;

        public TestingController(IFakeSnapshotService fakeSnapshotService,
            MarginTradingSettings settings)
        {
            _fakeSnapshotService = fakeSnapshotService;
            _protectionKey = settings.TestSettings?.ProtectionKey;
        }

        /// <summary>
        /// Save snapshot of provided orders, positions, account stats, best fx prices, best trading prices.
        /// FOR TEST PURPOSES ONLY.
        /// </summary>
        /// <returns>Snapshot statistics.</returns>
        [HttpPost("snapshot")]
        public async Task<string> AddOrUpdateTradingDataSnapshot(
            [FromBody] AddOrUpdateFakeSnapshotRequest request,
            [FromQuery] string protectionKey)
        {
            var (isValid, message) = ValidateProtectionKey(protectionKey);
            if (!isValid) return message;

            if (request.TradingDay == default)
            {
                throw new Exception($"{nameof(request.TradingDay)} must be set");
            }

            if (string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                throw new Exception($"{nameof(request.CorrelationId)} must be set");
            }

            return await _fakeSnapshotService.AddOrUpdateFakeTradingDataSnapshot(request.TradingDay,
                request.CorrelationId,
                request.Orders,
                request.Positions,
                request.Accounts,
                request.BestFxPrices,
                request.BestTradingPrices);
        }

        /// <summary>
        /// Deletes trading data snapshot
        /// FOR TEST PURPOSES ONLY.
        /// </summary>
        [HttpDelete("snapshot")]
        public async Task<string> DeleteTradingDataSnapshot([FromQuery] string correlationId, [FromQuery] string protectionKey)
        {
            var (isValid, message) = ValidateProtectionKey(protectionKey);
            if (!isValid) return message;

            await _fakeSnapshotService.DeleteFakeTradingSnapshot(correlationId);
            return $"Snapshot {correlationId} deleted";
        }

        private (bool isValid, string message) ValidateProtectionKey(string protectionKey)
        {
            var configuredProtectionKey = _protectionKey;
            if (string.IsNullOrEmpty(configuredProtectionKey))
                return (false, "Test protection key must be configured to use this api");
            
            if (configuredProtectionKey != protectionKey)
                return (false, "Protection key is invalid");

            return (true, string.Empty);
        }
    }
}