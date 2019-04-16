using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Internal;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/service")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class ServiceController : Controller, IServiceApi
    {
        private readonly IOvernightMarginParameterContainer _overnightMarginParameterContainer;

        public ServiceController(
            IOvernightMarginParameterContainer overnightMarginParameterContainer)
        {
            _overnightMarginParameterContainer = overnightMarginParameterContainer;
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
                .OrderBy(x => x.Key.Item1)
                .ThenBy(x => x.Key.Item2)
                .ToDictionary(x => x.Key, x => x.Value);
            
            return Task.FromResult(result);
        }
    }
}