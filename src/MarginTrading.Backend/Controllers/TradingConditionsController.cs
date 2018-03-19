using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Middleware;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Contract.BackendContracts.TradingConditions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/tradingConditions")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class TradingConditionsController : Controller
    {
        private readonly TradingConditionsManager _tradingConditionsManager;
        private readonly AccountGroupManager _accountGroupManager;
        private readonly AccountAssetsManager _accountAssetsManager;

        public TradingConditionsController(TradingConditionsManager tradingConditionsManager,
            AccountGroupManager accountGroupManager,
            AccountAssetsManager accountAssetsManager)
        {
            _tradingConditionsManager = tradingConditionsManager;
            _accountGroupManager = accountGroupManager;
            _accountAssetsManager = accountAssetsManager;
        }

        [HttpPost]
        [Route("")]
        [SwaggerOperation("AddOrReplaceTradingCondition")]
        public async Task<MtBackendResponse<TradingConditionModel>> AddOrReplaceTradingCondition(
            [FromBody] TradingConditionModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Id)) 
                return MtBackendResponse<TradingConditionModel>.Error("Id cannot be empty"); 
             
            if (string.IsNullOrWhiteSpace(model.Name)) 
                return MtBackendResponse<TradingConditionModel>.Error("Name cannot be empty"); 
             
            if (string.IsNullOrWhiteSpace(model.LegalEntity)) 
                return MtBackendResponse<TradingConditionModel>.Error("LegalEntity cannot be empty"); 
 
 
            var tradingCondition = model.ToDomainContract(); 
 
            tradingCondition = await _tradingConditionsManager.AddOrReplaceTradingConditionAsync(tradingCondition); 
            
            return MtBackendResponse<TradingConditionModel>.Ok(tradingCondition?.ToBackendContract());
        }

        [HttpPost]
        [Route("accountGroups")]
        [SwaggerOperation("AddOrReplaceAccountGroup")]
        public async Task<MtBackendResponse<AccountGroupModel>> AddOrReplaceAccountGroup(
            [FromBody] AccountGroupModel model)
        {
            var accountGroup = await _accountGroupManager.AddOrReplaceAccountGroupAsync(model.ToDomainContract());

            return MtBackendResponse<AccountGroupModel>.Ok(accountGroup.ToBackendContract());
        }

        [HttpPost]
        [Route("accountAssets/assignInstruments")]
        [SwaggerOperation("AssignInstruments")]
        public async Task<MtBackendResponse<IEnumerable<AccountAssetPairModel>>> AssignInstruments(
            [FromBody] AssignInstrumentsRequest model)
        {
            try
            {
                var assetPairs = await _accountAssetsManager.AssignInstruments(model.TradingConditionId, model.BaseAssetId,
                    model.Instruments);
                
                return MtBackendResponse<IEnumerable<AccountAssetPairModel>>.Ok(
                    assetPairs.Select(a => a.ToBackendContract()));
            }
            catch (Exception e)
            {
                return MtBackendResponse<IEnumerable<AccountAssetPairModel>>.Error(e.Message);
            }
        }

        [HttpPost]
        [Route("accountAssets")]
        [SwaggerOperation("AddOrReplaceAccountAsset")]
        public async Task<MtBackendResponse<AccountAssetPairModel>> AddOrReplaceAccountAsset([FromBody]AccountAssetPairModel model)
        {
            var assetPair = await _accountAssetsManager.AddOrReplaceAccountAssetAsync(model.ToDomainContract());

            return MtBackendResponse<AccountAssetPairModel>.Ok(assetPair.ToBackendContract());
        }
    }
}