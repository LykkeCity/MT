// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Services
{
    public interface IAccountsProvider
    {
        MarginTradingAccount GetAccountById(string accountId);
        
        bool TryFinishLiquidation(string accountId, string reason, string liquidationOperationId = null);
    }
}