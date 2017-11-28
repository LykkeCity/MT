using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Contract.BackendContracts.TradingConditions;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/tradingConditions")]
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
        [Route("~/api/backoffice/tradingConditions/add")]
        public async Task<MtBackendResponse<TradingConditionModel>> AddOrReplaceTradingCondition(
            [FromBody] TradingConditionModel model)
        {
            var tradingCondition = model.ToDomainContract();

            tradingCondition = await _tradingConditionsManager.AddOrReplaceTradingConditionAsync(tradingCondition);
            
            return MtBackendResponse<TradingConditionModel>.Ok(tradingCondition?.ToBackendContract());
        }

        [HttpPost]
        [Route("accountGroups")]
        [Route("~/api/backoffice/accountGroups/add")]
        public async Task<MtBackendResponse<AccountGroupModel>> AddOrReplaceAccountGroup(
            [FromBody] AccountGroupModel model)
        {
            var accountGroup = await _accountGroupManager.AddOrReplaceAccountGroupAsync(model.ToDomainContract());

            return MtBackendResponse<AccountGroupModel>.Ok(accountGroup.ToBackendContract());
        }

        [HttpPost]
        [Route("accountAssets/assignInstruments")]
        [Route("~/api/backoffice/accountAssets/assignInstruments")]
        public async Task<MtBackendResponse<IEnumerable<AccountAssetPairModel>>> AssignInstruments(
            [FromBody] AssignInstrumentsRequest model)
        {
            var assetPairs = await _accountAssetsManager.AssignInstruments(model.TradingConditionId, model.BaseAssetId,
                model.Instruments);

            return MtBackendResponse<IEnumerable<AccountAssetPairModel>>.Ok(
                assetPairs.Select(a => a.ToBackendContract()));
        }

        [HttpPost]
        [Route("accountAssets")]
        [Route("~/api/backoffice/accountAssets/add")]
        public async Task<MtBackendResponse<AccountAssetPairModel>> AddOrReplaceAccountAsset([FromBody]AccountAssetPairModel model)
        {
            var assetPair = await _accountAssetsManager.AddOrReplaceAccountAssetAsync(model.ToDomainContract());

            return MtBackendResponse<AccountAssetPairModel>.Ok(assetPair.ToBackendContract());
        }
    }
}