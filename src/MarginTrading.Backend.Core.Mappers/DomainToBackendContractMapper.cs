using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Backend.Core.Mappers
{
    public static class DomainToBackendContractMapper
    {
        public static MarginTradingAccountBackendContract ToFullBackendContract(this IMarginTradingAccount src, bool isLive)
        {
            return new MarginTradingAccountBackendContract
            {
                Id = src.Id,
                ClientId = src.ClientId,
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                MarginCall = src.GetMarginCall1Level(),
                StopOut = src.GetStopOutLevel(),
                TotalCapital = src.GetTotalCapital(),
                FreeMargin = src.GetFreeMargin(),
                MarginAvailable = src.GetMarginAvailable(),
                UsedMargin = src.GetUsedMargin(),
                MarginInit = src.GetMarginInit(),
                PnL = src.GetPnl(),
                OpenPositionsCount = src.GetOpenPositionsCount(),
                MarginUsageLevel = src.GetMarginUsageLevel(),
                IsLive = isLive,
                LegalEntity = src.LegalEntity,
            };
        }
    }
}
