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

        public ServiceController(
            IOvernightMarginParameterContainer overnightMarginParameterContainer,
            IIdentityGenerator identityGenerator,
            ISnapshotService snapshotService)
        {
            _overnightMarginParameterContainer = overnightMarginParameterContainer;
            _identityGenerator = identityGenerator;
            _snapshotService = snapshotService;
        }

        /// <summary>
        /// Save snapshot of orders, positions, account stats, best fx prices, best trading prices for current moment.
        /// Throws an error in case if trading is not stopped.
        /// </summary>
        /// <returns>Snapshot statistics.</returns>
        [HttpPost("make-trading-data-snapshot")]
        public async Task<string> MakeTradingDataSnapshot([FromQuery] string correlationId = null)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = _identityGenerator.GenerateGuid();
            }
            
            return await _snapshotService.MakeTradingDataSnapshot(correlationId);
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
        [HttpGet("overnight-margin-parameter")]
        public Task<Dictionary<(string, string), decimal>> GetOvernightMarginParameterValues(
            [FromQuery] string[] instruments = null)
        {
            var result = _overnightMarginParameterContainer.GetOvernightMarginParameterValues()
                .Where(x => instruments == null || !instruments.Any() || instruments.Contains(x.Key.Item2))
                .OrderBy(x => x.Value > 1 ? 0 : 1)
                .ThenBy(x => x.Key.Item1)
                .ThenBy(x => x.Key.Item2)
                .ToDictionary(x => x.Key, x => x.Value);
            
            return Task.FromResult(result);
        }
    }
}