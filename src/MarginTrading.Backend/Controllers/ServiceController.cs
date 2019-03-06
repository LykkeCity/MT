using System;
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
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IOvernightMarginParameterContainer _overnightMarginParameterContainer;
        private readonly IOvernightMarginRepository _overnightMarginRepository;
        private readonly ICqrsSender _cqrsSender;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly IDateService _dateService;
        private readonly OrdersCache _ordersCache;

        public ServiceController(
            IQuoteCacheService quoteCacheService,
            IOvernightMarginParameterContainer overnightMarginParameterContainer,
            IOvernightMarginRepository overnightMarginRepository,
            ICqrsSender cqrsSender,
            IIdentityGenerator identityGenerator,
            IDateService dateService,
            OrdersCache ordersCache)
        {
            _quoteCacheService = quoteCacheService;
            _overnightMarginParameterContainer = overnightMarginParameterContainer;
            _overnightMarginRepository = overnightMarginRepository;
            _cqrsSender = cqrsSender;
            _identityGenerator = identityGenerator;
            _dateService = dateService;
            _ordersCache = ordersCache;
        }

        [HttpDelete]
        [Route("bestprice/{assetPair}")]
        public MtBackendResponse<bool> ClearBestBriceCache(string assetPair)
        {
            var positions = _ordersCache.Positions.GetPositionsByInstrument(assetPair).ToList();
            if (positions.Any())
            {
                return MtBackendResponse<bool>.Error(
                    $"Cannot delete [{assetPair}] best price because there are {positions.Count} opened positions.");
            }
            
            var orders = _ordersCache.Active.GetOrdersByInstrument(assetPair).ToList();
            if (orders.Any())
            {
                return MtBackendResponse<bool>.Error(
                    $"Cannot delete [{assetPair}] best price because there are {orders.Count} active orders.");
            }
            
            _quoteCacheService.RemoveQuote(assetPair);
            
            return MtBackendResponse<bool>.Ok(true);
        }

        /// <summary>
        /// Get current value of overnight margin parameter.
        /// </summary>
        [HttpGet("current-overnight-margin-parameter")]
        public Task<decimal> GetCurrentOvernightMarginParameter()
        {
            return Task.FromResult(_overnightMarginParameterContainer.OvernightMarginParameter);
        }

        /// <summary>
        /// Get persisted value of overnight margin parameter.
        /// This value is applied at corresponding time, which depends on settings.
        /// </summary>
        [HttpGet("overnight-margin-parameter")]
        public Task<decimal> GetOvernightMarginParameter()
        {
            return Task.FromResult(_overnightMarginRepository.ReadOvernightMarginParameter());
        }
        
        /// <summary>
        /// Set and persist new value of overnight margin parameter.
        /// </summary>
        [HttpPut("overnight-margin-parameter")]
        public async Task SetOvernightMarginParameter(decimal newValue, string correlationId = null)
        {
            if (newValue <= 0 || newValue > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(newValue),
                    "Overnight margin parameter value must be > 0 and <= 100");
            }

            correlationId = correlationId ?? _identityGenerator.GenerateAlphanumericId();

            var changedActualValue = false;
            if (_overnightMarginParameterContainer.OvernightMarginParameter != 1)
            {
                _overnightMarginParameterContainer.OvernightMarginParameter = newValue;
                changedActualValue = true;
            }

            var oldValue = _overnightMarginRepository.ReadOvernightMarginParameter();

            await _overnightMarginRepository.WriteOvernightMarginParameterAsync(newValue);
            
            _cqrsSender.PublishEvent(new OvernightMarginParameterChangedEvent
            {
                CorrelationId = correlationId,
                EventTimestamp = _dateService.Now(),
                OldValue = oldValue,
                NewValue = newValue,
                ChangedActualValue = changedActualValue,
            });
        }
    }
}