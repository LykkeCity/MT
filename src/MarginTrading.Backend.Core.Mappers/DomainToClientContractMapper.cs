using MarginTrading.Contract.ClientContracts;

namespace MarginTrading.Backend.Core.Mappers
{
    public static class DomainToClientContractMapper
    {
        public static MarginTradingAccountClientContract ToClientContract(this MarginTradingAccount src)
        {
            return new MarginTradingAccountClientContract
            {
                Id = src.Id,
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                MarginCall = src.GetMarginCallLevel(),
                StopOut = src.GetStopOutLevel(),
                TotalCapital = src.GetTotalCapital(),
                FreeMargin = src.GetFreeMargin(),
                MarginAvailable = src.GetMarginAvailable(),
                UsedMargin = src.GetUsedMargin(),
                MarginInit = src.GetMarginInit(),
                PnL = src.GetPnl(),
                OpenPositionsCount = src.GetOpenPositionsCount(),
                MarginUsageLevel = src.GetMarginUsageLevel()
            };
        }

        public static BidAskClientContract ToClientContract(this InstrumentBidAskPair src)
        {
            return new BidAskClientContract
            {
                Id = src.Instrument,
                Date = src.Date,
                Bid = src.Bid,
                Ask = src.Ask
            };
        }
    }
}
