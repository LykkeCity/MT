using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories.Snow.Trades;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Trades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    /// <summary>
    /// Provides data about trades
    /// </summary>
    [Authorize]
    [Route("api/trades")]
    public class TradesController : Controller, ITradesApi
    {
        private readonly ITradesRepository _tradesRepository;

        public TradesController(ITradesRepository tradesRepository)
        {
            _tradesRepository = tradesRepository;
        }

        /// <summary>
        /// Get a trade by id 
        /// </summary>
        [HttpGet, Route("{tradeId}")]
        public async Task<TradeContract> Get(string tradeId)
        {
            return Convert(await _tradesRepository.GetAsync(tradeId));
        }

        /// <summary>
        /// Get trades with optional filtering by order or position 
        /// </summary>
        [HttpGet, Route("")]
        public async Task<List<TradeContract>> List([FromQuery] string orderId, [FromQuery] string positionId)
        {
            if (orderId == null && positionId == null)
                throw new ArgumentException($"{nameof(orderId)} or {nameof(positionId)} should be passed");

            if (orderId != null && positionId != null && orderId != positionId)
                throw new ArgumentException(
                    $"{nameof(orderId)} and {nameof(positionId)} should be equal if both passed, separation is not yet supported");

            var id = orderId ?? positionId;
            return new List<TradeContract> {Convert(await _tradesRepository.GetAsync(id))};
        }

        private TradeContract Convert(ITrade tradeEntity)
        {
            return new TradeContract
            {
                // todo: separate order from position and trade and use there ids correctly
                Id = tradeEntity.Id,
                AccountId = tradeEntity.AccountId,
                OrderId = tradeEntity.Id,
                PositionId = tradeEntity.Id,
                Timestamp = tradeEntity.TradeTimestamp,
            };
        }
    }
}