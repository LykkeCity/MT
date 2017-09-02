using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/tradingConditions")]
    public class TradingConditionsController : Controller
    {
        private readonly IMarginTradingConditionRepository _conditionsRepository;

        public TradingConditionsController(IMarginTradingConditionRepository conditionsRepository)
        {
            _conditionsRepository = conditionsRepository;
        }

        /// <summary>
        /// Returns all trading conditions
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<MarginTradingCondition>> GetAllTradingConditions()
        {
            return (await _conditionsRepository.GetAllAsync()).Select(MarginTradingCondition.Create);
        }

        /// <summary>
        /// Returns trading condition by id
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public async Task<MarginTradingCondition> GetTradingConditionById(string id)
        {
            return MarginTradingCondition.Create(await _conditionsRepository.GetAsync(id));
        }
    }
}
