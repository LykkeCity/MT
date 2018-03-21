using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.TradingConditions;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/tradingConditions")]
    public class TradingConditionsController : Controller, ITradingConditionsReadingApi
    {
        private readonly ITradingConditionRepository _conditionsRepository;
        private readonly IConvertService _convertService;

        public TradingConditionsController(ITradingConditionRepository conditionsRepository, IConvertService convertService)
        {
            _conditionsRepository = conditionsRepository;
            _convertService = convertService;
        }

        /// <summary>
        /// Returns all trading conditions
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<List<TradingConditionContract>> List()
        {
            return (await _conditionsRepository.GetAllAsync()).Select(Convert).ToList();
        }

        /// <summary>
        /// Returns trading condition by id
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public async Task<TradingConditionContract> Get(string id)
        {
            return Convert(await _conditionsRepository.GetAsync(id));
        }


        private TradingConditionContract Convert(ITradingCondition tradingCondition)
        {
            return _convertService.Convert<ITradingCondition, TradingConditionContract>(tradingCondition);
        }
    }
}
