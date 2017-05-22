using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using MarginTrading.Core;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/watchlists")]
    [Authorize]
    public class WatchListsController : Controller
    {
        private readonly IWatchListService _watchListService;

        public WatchListsController(IWatchListService watchListService)
        {
            _watchListService = watchListService;
        }

        [HttpGet]
        [Route("")]
        public async Task<ResponseModel<List<MarginTradingWatchList>>> GetWatchLists()
        {
            var clientId = User.GetClaim(ClaimTypes.NameIdentifier);

            if (clientId == null)
            {
                return ResponseModel<List<MarginTradingWatchList>>.CreateFail(ResponseModel.ErrorCodeType.NoAccess, "Wrong token");
            }

            var result = new ResponseModel<List<MarginTradingWatchList>>
            {
                Result = await _watchListService.GetAllAsync(clientId)
            };

            return result;
        }

        [HttpPost]
        [Route("")]
        public async Task<ResponseModel<IMarginTradingWatchList>> AddWatchList([FromBody]WatchList model)
        {
            var clientId = User.GetClaim(ClaimTypes.NameIdentifier);

            if (clientId == null)
            {
                return ResponseModel<IMarginTradingWatchList>.CreateFail(ResponseModel.ErrorCodeType.NoAccess, "Wrong token");
            }

            var result = new ResponseModel<IMarginTradingWatchList>();

            if (model.AssetIds == null || model.AssetIds.Count == 0)
            {
                return ResponseModel<IMarginTradingWatchList>.CreateInvalidFieldError("AssetIds", "AssetIds should not be empty");
            }

            var addResult = await _watchListService.AddAsync(model.Id, clientId, model.Name, model.AssetIds);

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
        [Route("{id}")]
        public async Task<ResponseModel> DeleteWatchList(string id)
        {
            var clientId = User.GetClaim(ClaimTypes.NameIdentifier);

            if (clientId == null)
            {
                return ResponseModel<List<MarginTradingWatchList>>.CreateFail(ResponseModel.ErrorCodeType.NoAccess, "Wrong token");
            }

            var result = await _watchListService.DeleteAsync(clientId, id);

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
