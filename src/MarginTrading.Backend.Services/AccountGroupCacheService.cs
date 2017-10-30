﻿using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services
{
    public class AccountGroupCacheService : IAccountGroupCacheService
    {
        private List<IMarginTradingAccountGroup> _accountGroups = new List<IMarginTradingAccountGroup>();

        public IMarginTradingAccountGroup[] GetAllAccountGroups()
        {
            return _accountGroups.ToArray();
        }

        public IMarginTradingAccountGroup GetAccountGroup(string tradingConditionId, string accountAssetId)
        {
            return _accountGroups.FirstOrDefault(item => item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId);
        }

        internal void InitAccountGroupsCache(List<IMarginTradingAccountGroup> accountGroups)
        {
            _accountGroups = accountGroups;
        }
    }
}
