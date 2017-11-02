using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public class AccountGroupCacheService : IAccountGroupCacheService
    {
        private List<IAccountGroup> _accountGroups = new List<IAccountGroup>();

        public IAccountGroup[] GetAllAccountGroups()
        {
            return _accountGroups.ToArray();
        }

        public IAccountGroup GetAccountGroup(string tradingConditionId, string accountAssetId)
        {
            return _accountGroups.FirstOrDefault(item => item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId);
        }

        internal void InitAccountGroupsCache(List<IAccountGroup> accountGroups)
        {
            _accountGroups = accountGroups;
        }
    }
}
