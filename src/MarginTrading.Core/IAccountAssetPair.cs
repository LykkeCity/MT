using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core.Settings;

namespace MarginTrading.Core
{
    public interface IAccountAssetPair
    {
        string TradingConditionId { get; }
        string BaseAssetId { get; }
        string Instrument { get; }
        int LeverageInit { get; }
        int LeverageMaintenance { get; }
        decimal SwapLong { get; }
        decimal SwapShort { get; }
        decimal SwapLongPct { get; }
        decimal SwapShortPct { get; }
        decimal CommissionLong { get; }
        decimal CommissionShort { get; }
        decimal CommissionLot { get; }
        decimal DeltaBid { get; }
        decimal DeltaAsk { get; }
        decimal DealLimit { get; }
        decimal PositionLimit { get; }
    }

    public class AccountAssetPair : IAccountAssetPair
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string Instrument { get; set; }
        public int LeverageInit { get; set; }
        public int LeverageMaintenance { get; set; }
        public decimal SwapLong { get; set; }
        public decimal SwapShort { get; set; }
        public decimal SwapLongPct { get; set; }
        public decimal SwapShortPct { get; set; }
        public decimal CommissionLong { get; set; }
        public decimal CommissionShort { get; set; }
        public decimal CommissionLot { get; set; }
        public decimal DeltaBid { get; set; }
        public decimal DeltaAsk { get; set; }
        public decimal DealLimit { get; set; }
        public decimal PositionLimit { get; set; }

        public static AccountAssetPair Create(IAccountAssetPair src)
        {
            return new AccountAssetPair
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Instrument = src.Instrument,
                LeverageInit = src.LeverageInit,
                LeverageMaintenance = src.LeverageMaintenance,
                SwapLong = src.SwapLong,
                SwapShort = src.SwapShort,
                SwapLongPct = src.SwapLongPct,
                SwapShortPct = src.SwapShortPct,
                CommissionLong = src.CommissionLong,
                CommissionShort = src.CommissionShort,
                CommissionLot = src.CommissionLot,
                DeltaBid = src.DeltaBid,
                DeltaAsk = src.DeltaAsk,
                DealLimit = src.DealLimit,
                PositionLimit = src.PositionLimit
            };
        }
    }

    public interface IAccountAssetPairsRepository
    {
        Task AddOrReplaceAsync(IAccountAssetPair accountAssetPair);
        Task<IAccountAssetPair> GetAsync(string tradingConditionId, string baseAssetId, string assetPairId);
        Task<IEnumerable<IAccountAssetPair>> GetAllAsync(string tradingConditionId, string baseAssetId);
        Task<IEnumerable<IAccountAssetPair>> GetAllAsync();
        Task AssignAssetPairs(string tradingConditionId, string baseAssetId, string[] assetPairsIds, AccountAssetsSettings defaults);
    }
}
