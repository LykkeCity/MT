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
        double SwapLong { get; }
        double SwapShort { get; }
        double SwapLongPct { get; }
        double SwapShortPct { get; }
        double CommissionLong { get; }
        double CommissionShort { get; }
        double CommissionLot { get; }
        double DeltaBid { get; }
        double DeltaAsk { get; }
        double DealLimit { get; }
        double PositionLimit { get; }
    }

    public class AccountAssetPair : IAccountAssetPair
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string Instrument { get; set; }
        public int LeverageInit { get; set; }
        public int LeverageMaintenance { get; set; }
        public double SwapLong { get; set; }
        public double SwapShort { get; set; }
        public double SwapLongPct { get; set; }
        public double SwapShortPct { get; set; }
        public double CommissionLong { get; set; }
        public double CommissionShort { get; set; }
        public double CommissionLot { get; set; }
        public double DeltaBid { get; set; }
        public double DeltaAsk { get; set; }
        public double DealLimit { get; set; }
        public double PositionLimit { get; set; }

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
