using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.TradingConditions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/tradingConditions")]
    public class TradingConditionsController : Controller
    {
        private readonly ITradingConditionRepository _conditionsRepository;

        public TradingConditionsController(ITradingConditionRepository conditionsRepository)
        {
            _conditionsRepository = conditionsRepository;
        }

        /// <summary>
        /// Returns all trading conditions
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<TradingCondition>> GetAllTradingConditions()
        {
            return (await _conditionsRepository.GetAllAsync()).Select(TradingCondition.Create);
        }

        /// <summary>
        /// Returns trading condition by id
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public async Task<TradingCondition> GetTradingConditionById(string id)
        {
            return TradingCondition.Create(await _conditionsRepository.GetAsync(id));
        }
    }
}
