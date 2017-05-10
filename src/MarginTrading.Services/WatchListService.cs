using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class WatchListService : IWatchListService
    {
        private readonly IMarginTradingAccountsRepository _accountsRepository;
        private readonly IMarginTradingAccountAssetRepository _accountAssetRepository;
        private readonly IMarginTradingWatchListRepository _watchListRepository;
        private const string AllAssetsWatchListId = "all_assets_watchlist";

        public WatchListService(
            IMarginTradingAccountsRepository accountsRepository,
            IMarginTradingAccountAssetRepository accountAssetRepository,
            IMarginTradingWatchListRepository watchListRepository)
        {
            _accountsRepository = accountsRepository;
            _accountAssetRepository = accountAssetRepository;
            _watchListRepository = watchListRepository;
        }

        public async Task<List<MarginTradingWatchList>> GetAllAsync(string accountId)
        {
            if (IsAccountExists(accountId))
            {
                return await GetWatchLists(accountId);
            }

            return null;
        }

        public async Task<IMarginTradingWatchList> GetAsync(string accountId, string id)
        {
            return id == AllAssetsWatchListId
                ? await GetAllAssetsWatchList(accountId)
                : await _watchListRepository.GetAsync(accountId, id);
        }

        public async Task<WatchListResult<IMarginTradingWatchList>> AddAsync(string id, string accountId, string name, List<string> assetIds)
        {
            var result = new WatchListResult<IMarginTradingWatchList>();
            bool isNew = string.IsNullOrEmpty(id);
            var watchLists = (await GetWatchLists(accountId)).ToList();
            var allAssets = await GetAccountAssetIds(accountId);

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
                AccountId = accountId,
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

        public async Task<WatchListResult<bool>> DeleteAsync(string accountId, string id)
        {
            var result = new WatchListResult<bool>();

            var watchList = await GetAsync(accountId, id);

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

            await _watchListRepository.DeleteAsync(accountId, id);

            result.Result = true;
            return result;
        }

        private async Task<List<string>> GetAccountAssetIds(string accountId)
        {
            var result = new List<string>();

            var account = await _accountsRepository.GetAsync(accountId);

            if (account != null)
            {
                var assets = await _accountAssetRepository.GetAllAsync(account.TradingConditionId, account.BaseAssetId);
                result = assets.Select(item => item.Instrument).ToList();
            }

            return result;
        }

        private bool IsAccountExists(string accountId)
        {
            var account = _accountsRepository.GetAsync(accountId).Result;
            return account != null;
        }

        private async Task<List<MarginTradingWatchList>> GetWatchLists(string accountId)
        {
            var result = new List<MarginTradingWatchList>();
            var allAssets = await GetAccountAssetIds(accountId);

            var watchLists = (await _watchListRepository.GetAllAsync(accountId)).ToList();

            if (watchLists.Any())
            {
                foreach (var watchlist in watchLists)
                {
                    watchlist.AssetIds.RemoveAll(item => !allAssets.Contains(item));
                }

                result.AddRange(watchLists.Select(MarginTradingWatchList.Create));
            }

            var watchList = await GetAllAssetsWatchList(accountId);

            result.Insert(0, watchList);

            return result;
        }

        private async Task<MarginTradingWatchList> GetAllAssetsWatchList(string accountId)
        {
            var allAssets = await GetAccountAssetIds(accountId);

            return new MarginTradingWatchList
            {
                Id = AllAssetsWatchListId,
                AccountId = accountId,
                Name = "All assets",
                AssetIds = allAssets,
                ReadOnly = true
            };
        }
    }
}
