using System;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    public class AccountUpdateService : IAccountUpdateService
    {
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IAccountGroupCacheService _accountGroupCacheService;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetsCache _assetsCache;

        public AccountUpdateService(
            ICfdCalculatorService cfdCalculatorService,
            IAccountGroupCacheService accountGroupCacheService,
            IAccountAssetsCacheService accountAssetsCacheService,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IAssetsCache assetsCache)
        {
            _cfdCalculatorService = cfdCalculatorService;
            _accountGroupCacheService = accountGroupCacheService;
            _accountAssetsCacheService = accountAssetsCacheService;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _assetsCache = assetsCache;
        }

        public void UpdateAccount(IMarginTradingAccount account, AccountFpl accountFpl, Order[] orders = null)
        {
            if (orders == null)
            {
                orders = _ordersCache.ActiveOrders.GetOrdersByAccountIds(account.Id).ToArray();
            }

            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            
            accountFpl.PnL = Math.Round(orders.Sum(x => x.GetTotalFpl()), accuracy);

            accountFpl.UsedMargin = Math.Round(orders.Sum(item => item.GetMarginMaintenance()),
                accuracy);
            accountFpl.MarginInit = Math.Round(orders.Sum(item => item.GetMarginInit()),
                accuracy);
            accountFpl.OpenPositionsCount = orders.Length;

            var accountGroup = _accountGroupCacheService.GetAccountGroup(account.TradingConditionId, account.BaseAssetId);

            if (accountGroup == null)
            {
                throw new Exception(string.Format(MtMessages.AccountGroupForTradingConditionNotFound, account.TradingConditionId, account.BaseAssetId));
            }

            accountFpl.MarginCallLevel = accountGroup.MarginCall;
            accountFpl.StopoutLevel = accountGroup.StopOut;
            accountFpl.CalculatedHash = accountFpl.ActualHash;
        }

        public bool IsEnoughBalance(Order order)
        {
            var volumeInAccountAsset = _cfdCalculatorService.GetVolumeInAccountAsset(order.GetOrderType(),
                order.AccountAssetId, order.Instrument, Math.Abs(order.Volume), order.LegalEntity);
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId, order.AccountAssetId, order.Instrument);

            return account.GetMarginAvailable() * accountAsset.LeverageInit >= volumeInAccountAsset;
        }

        public MarginTradingAccount GuessAccountWithOrder(Order order)
        {
            var accountFpl = new AccountFpl();
            var newInstance = MarginTradingAccount.Create(_accountsCacheService.Get(order.ClientId, order.AccountId), accountFpl);

            var orders = _ordersCache.ActiveOrders.GetOrdersByInstrumentAndAccount(order.Instrument, order.AccountId).ToList();
            orders.Add(order);

            UpdateAccount(newInstance, accountFpl, orders.ToArray());

            return newInstance;
        }
    }
}
