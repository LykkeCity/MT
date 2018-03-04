using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Common.Settings;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Frontend.Repositories;
using MarginTrading.Frontend.Repositories.Contract;

namespace MarginTrading.Frontend.Services
{
    public class WatchListService : IWatchListService
    {
        private readonly IHttpRequestService _httpRequestService;
        private readonly IMarginTradingWatchListRepository _watchListRepository;
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;
        private const string AllAssetsWatchListId = "all_assets_watchlist";

        public WatchListService(
            IHttpRequestService httpRequestService,
            IMarginTradingWatchListRepository watchListRepository,
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService)
        {
            _httpRequestService = httpRequestService;
            _watchListRepository = watchListRepository;
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
        }

        public async Task<List<MarginTradingWatchList>> GetAllAsync(string clientId)
        {
            return await GetWatchLists(clientId);
        }

        public async Task<IMarginTradingWatchList> GetAsync(string clientId, string id)
        {
            return id == AllAssetsWatchListId
                ? await GetAllAssetsWatchList(clientId)
                : await _watchListRepository.GetAsync(clientId, id);
        }

        public async Task<WatchListResult<IMarginTradingWatchList>> AddAsync(string id, string clientId, string name, List<string> assetIds)
        {
            var result = new WatchListResult<IMarginTradingWatchList>();
            var isNew = string.IsNullOrEmpty(id);
            var watchLists = (await GetWatchLists(clientId)).ToList();
            var allAssets = await GetAvailableAssetIds(clientId);

            foreach (var assetId in assetIds)
            {
                if (!allAssets.Contains(assetId))
                {
                    result.Status = WatchListStatus.AssetNotFound;
                    result.Message = assetId;
                    return result;
                }
            }

            var existing = watchLists.FirstOrDefault(item => item.Id == id);

            if (existing != null && existing.ReadOnly)
            {
                result.Status = WatchListStatus.ReadOnly;
                result.Message = "This watch list is readonly";
                return result;
            }

            var watchList = new MarginTradingWatchList
            {
                Id = isNew ? Guid.NewGuid().ToString("N") : id,
                ClientId = clientId,
                Name = name,
                AssetIds = assetIds
            };

            if (isNew)
            {
                watchList.Order = watchLists.Count;
            }

            if (existing != null)
            {
                watchList.Order = existing.Order;
            }

            result.Result = await _watchListRepository.AddAsync(watchList);
            return result;
        }

        public async Task<WatchListResult<bool>> DeleteAsync(string clientId, string id)
        {
            var result = new WatchListResult<bool>();

            var watchList = await GetAsync(clientId, id);

            if (watchList == null)
            {
                result.Status = WatchListStatus.NotFound;
                return result;
            }

            if (watchList.ReadOnly)
            {
                result.Status = WatchListStatus.ReadOnly;
                return result;
            }

            await _watchListRepository.DeleteAsync(clientId, id);

            result.Result = true;
            return result;
        }

        private async Task<List<string>> GetAvailableAssetIds(string clientId)
        {

            var marginTradingEnabled = await _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId);
            var responses = await _httpRequestService.RequestIfAvailableAsync(new ClientIdBackendRequest { ClientId = clientId },
                                                                              "init.availableassets",
                                                                              () => new List<string>(),
                                                                              marginTradingEnabled);
            return responses.Live.Concat(responses.Demo).Distinct().ToList();
        }

        private async Task<List<MarginTradingWatchList>> GetWatchLists(string clientId)
        {
            var availableAssets = await GetAvailableAssetIds(clientId);

            var result = new List<MarginTradingWatchList>();

            var watchLists = (await _watchListRepository.GetAllAsync(clientId)).ToList();

            if (watchLists.Any())
            {
                foreach (var watchlist in watchLists)
                {
                    watchlist.AssetIds.RemoveAll(item => !availableAssets.Contains(item));
                }

                result.AddRange(watchLists.Select(MarginTradingWatchList.Create));
            }

            var watchList = await GetAllAssetsWatchList(clientId);

            result.Insert(0, watchList);

            return result;
        }

        private async Task<MarginTradingWatchList> GetAllAssetsWatchList(string clientId)
        {
            var allAssets = await GetAvailableAssetIds(clientId);

            return new MarginTradingWatchList
            {
                Id = AllAssetsWatchListId,
                ClientId = clientId,
                Name = "All assets",
                AssetIds = allAssets,
                ReadOnly = true
            };
        }
    }
}
