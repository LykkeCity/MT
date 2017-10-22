using System.Collections.Generic;
using System.Collections.Immutable;
using MarginTrading.MarketMaker.Models.Api;
using MarginTrading.MarketMaker.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;

namespace MarginTrading.MarketMaker.Controllers
{
    [Route("api/[controller]")]
    public class TestsController : Controller
    {
        private readonly ITestingHelperService _testingHelperService;

        public TestsController(ITestingHelperService testingHelperService)
        {
            _testingHelperService = testingHelperService;
        }

        /// <summary>
        /// Adds settings
        /// </summary>
        [HttpPost]
        [Route("add")]
        [SwaggerOperation("AddTestSettings")]
        public IActionResult Add([FromBody] ImmutableList<TestSetting> settings)
        {
            _testingHelperService.Add(settings);
            return Ok(new {success = true});
        }


        /// <summary>
        /// Deletes settings
        /// </summary>
        [HttpPost]
        [Route("delete")]
        [SwaggerOperation("DeleteAllTestSettings")]
        public IActionResult DeleteAll()
        {
            _testingHelperService.DeleteAll();
            return Ok(new {success = true});
        }

        /// <summary>
        /// Deletes settings
        /// </summary>
        [HttpPost]
        [Route("delete/{assetPairId}/{exchange}")]
        [SwaggerOperation("DeleteTestSettings")]
        public IActionResult Delete(string assetPairId, string exchange)
        {
            _testingHelperService.Delete(assetPairId, exchange);
            return Ok(new {success = true});
        }

        /// <summary>
        /// Gets all existing settings
        /// </summary>
        [HttpGet]
        [Route("")]
        [SwaggerOperation("GetAllTestSettings")]
        public IReadOnlyDictionary<(string AssetPairId, string Exchange), ImmutableList<TestSetting>> GetAll() => _testingHelperService.GetAll();

        /// <summary>
        /// Set settings
        /// </summary>
        [HttpGet]
        [Route("{assetPairId}/{exchange}")]
        [SwaggerOperation("GetTestSettings")]
        public ImmutableList<TestSetting> Get(string assetPairId, string exchange)
        {
            return _testingHelperService.Get(assetPairId, exchange);
        }
    }
}