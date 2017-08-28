using System.Collections.Generic;
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
        public Task<IEnumerable<IMarginTradingCondition>> GetAll()
        {
            return _conditionsRepository.GetAllAsync();
        }

        /// <summary>
        /// Returns trading condition by id
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public Task<IMarginTradingCondition> GetById(string id)
        {
            return _conditionsRepository.GetAsync(id);
        }
    }
}
