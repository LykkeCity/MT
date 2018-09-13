using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using MarginTrading.Common.Middleware;
using MarginTrading.Frontend.Infrastructure;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Repositories;
using MarginTrading.Frontend.Repositories.Contract;
using MarginTrading.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/watchlists")]
    [Authorize]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class WatchListsController : Controller
    {
        private readonly IWatchListService _watchListService;

        public WatchListsController(IWatchListService watchListService)
        {
            _watchListService = watchListService;
        }

        [HttpGet]
        [Route("")]
        public async Task<ResponseModel<List<WatchListContract>>> GetWatchLists()
        {
            var clientId = User.GetClaim(AuthConsts.SubjectClaim);

            if (clientId == null)
            {
                return ResponseModel<List<WatchListContract>>.CreateFail(ResponseModel.ErrorCodeType.NoAccess, "Wrong token");
            }

            var result = new ResponseModel<List<WatchListContract>>
            {
                Result = Convert(await _watchListService.GetAllAsync(clientId))
            };

            return result;
        }

        [HttpPost]
        [Route("")]
        public async Task<ResponseModel<WatchListContract>> AddWatchList([FromBody]AddWatchListRequest model)
        {
            var clientId = User.GetClaim(AuthConsts.SubjectClaim);

            if (clientId == null)
            {
                return ResponseModel<WatchListContract>.CreateFail(ResponseModel.ErrorCodeType.NoAccess, "Wrong token");
            }

            var result = new ResponseModel<WatchListContract>();

            if (model.AssetIds == null || model.AssetIds.Count == 0)
            {
                return ResponseModel<WatchListContract>.CreateInvalidFieldError("AssetIds", "AssetIds should not be empty");
            }

            var addResult = await _watchListService.AddAsync(model.Id, clientId, model.Name, model.AssetIds);

            switch (addResult.Status)
            {
                case WatchListStatus.AssetNotFound:
                    return ResponseModel<WatchListContract>.CreateFail(ResponseModel.ErrorCodeType.AssetNotFound, $"Asset '{addResult.Message}' is not found or not allowed");
                case WatchListStatus.ReadOnly:
                    return ResponseModel<WatchListContract>.CreateFail(ResponseModel.ErrorCodeType.InconsistentData, "This watch list is readonly");
            }

            result.Result = Convert(addResult.Result);

            return result;
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<ResponseModel> DeleteWatchList(string id)
        {
            var clientId = User.GetClaim(AuthConsts.SubjectClaim);

            if (clientId == null)
            {
                return ResponseModel<List<WatchListContract>>.CreateFail(ResponseModel.ErrorCodeType.NoAccess, "Wrong token");
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

        private WatchListContract Convert(IMarginTradingWatchList wl)
        {
            return new WatchListContract
            {
                Id = wl.Id,
                Name = wl.Name,
                Order = wl.Order,
                ReadOnly = wl.ReadOnly,
                AssetIds = wl.AssetIds
            };
        }

        private List<WatchListContract> Convert(IEnumerable<IMarginTradingWatchList> wls)
        {
            return wls.Select(Convert).ToList();
        }
        
    }
}
