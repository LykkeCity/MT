using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Frontend.Models;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/watchlists")]
    public class WatchListsController : Controller
    {
        private readonly IWatchListService _watchListService;

        public WatchListsController(IWatchListService watchListService)
        {
            _watchListService = watchListService;
        }

        [HttpGet]
        [Route("{accountId}")]
        public async Task<ResponseModel<List<MarginTradingWatchList>>> GetWatchLists(string accountId)
        {
            var result = new ResponseModel<List<MarginTradingWatchList>>
            {
                Result = await _watchListService.GetAllAsync(accountId)
            };

            return result;
        }

        [HttpPost]
        [Route("")]
        public async Task<ResponseModel<IMarginTradingWatchList>> AddWatchList([FromBody]WatchList model)
        {
            var result = new ResponseModel<IMarginTradingWatchList>();

            if (string.IsNullOrEmpty(model.AccountId))
            {
                return ResponseModel<IMarginTradingWatchList>.CreateInvalidFieldError("AccountId", "AccountId should not be empty");
            }

            if (model.AssetIds == null || model.AssetIds.Count == 0)
            {
                return ResponseModel<IMarginTradingWatchList>.CreateInvalidFieldError("AssetIds", "AssetIds should not be empty");
            }

            var addResult = await _watchListService.AddAsync(model.Id, model.AccountId, model.Name, model.AssetIds);

            switch (addResult.Status)
            {
                case WatchListStatus.AssetNotFound:
                    return ResponseModel<IMarginTradingWatchList>.CreateFail(ResponseModel.ErrorCodeType.AssetNotFound, $"Asset '{addResult.Message}' is not found or not allowed");
                case WatchListStatus.ReadOnly:
                    return ResponseModel<IMarginTradingWatchList>.CreateFail(ResponseModel.ErrorCodeType.InconsistentData, "This watch list is readonly");
            }

            result.Result = addResult.Result;

            return result;
        }

        [HttpDelete]
        [Route("{accountId}/{id}")]
        public async Task<ResponseModel> DeleteWatchList(string accountId, string id)
        {
            var result = await _watchListService.DeleteAsync(accountId, id);

            switch (result.Status)
            {
                case WatchListStatus.NotFound:
                    return ResponseModel.CreateFail(ResponseModel.ErrorCodeType.NoData, "Watch list not found");
                case WatchListStatus.ReadOnly:
                    return ResponseModel.CreateFail(ResponseModel.ErrorCodeType.InconsistentData, "Readonly watch list can't be deleted");
            }

            return ResponseModel.CreateOk();
        }
    }
}
