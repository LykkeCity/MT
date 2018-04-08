using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.TradingConditions;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class AccountAssetPairEntity : TableEntity, IAccountAssetPair
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string Instrument => RowKey;
        public int LeverageInit { get; set; }
        public int LeverageMaintenance { get; set; }
        decimal IAccountAssetPair.SwapLong => (decimal) SwapLong;
        public double SwapLong { get; set; }
        decimal IAccountAssetPair.SwapShort => (decimal) SwapShort;
        public double SwapShort { get; set; }
        decimal IAccountAssetPair.OvernightSwapLong => (decimal) OvernightSwapLong;
        public double OvernightSwapLong { get; set; }
        decimal IAccountAssetPair.OvernightSwapShort => (decimal) OvernightSwapShort;
        public double OvernightSwapShort { get; set; }
        decimal IAccountAssetPair.CommissionLong => (decimal) CommissionLong;
        public double CommissionLong { get; set; }
        decimal IAccountAssetPair.CommissionShort => (decimal) CommissionShort;
        public double CommissionShort { get; set; }
        decimal IAccountAssetPair.CommissionLot => (decimal) CommissionLot;
        public double CommissionLot { get; set; }
        decimal IAccountAssetPair.DeltaBid => (decimal) DeltaBid;
        public double DeltaBid { get; set; }
        decimal IAccountAssetPair.DeltaAsk => (decimal) DeltaAsk;
        public double DeltaAsk { get; set; }
        decimal IAccountAssetPair.DealLimit => (decimal) DealLimit;
        public double DealLimit { get; set; }
        decimal IAccountAssetPair.PositionLimit => (decimal) PositionLimit;
        public double PositionLimit { get; set; }

        public static string GeneratePartitionKey(string tradingConditionId, string baseAssetId)
        {
            return $"{tradingConditionId}_{baseAssetId}";
        }

        public static string GenerateRowKey(string instrument)
        {
            return instrument;
        }

        public static AccountAssetPairEntity Create(IAccountAssetPair src)
        {
            return new AccountAssetPairEntity
            {
                PartitionKey = GeneratePartitionKey(src.TradingConditionId, src.BaseAssetId),
                RowKey = GenerateRowKey(src.Instrument),
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                LeverageInit = src.LeverageInit,
                LeverageMaintenance = src.LeverageMaintenance,
                SwapLong = (double) src.SwapLong,
                SwapShort = (double) src.SwapShort,
                OvernightSwapLong = (double) src.OvernightSwapLong,
                OvernightSwapShort = (double) src.OvernightSwapShort,
                CommissionLong = (double) src.CommissionLong,
                CommissionShort = (double) src.CommissionShort,
                CommissionLot = (double) src.CommissionLot,
                DeltaBid = (double) src.DeltaBid,
                DeltaAsk = (double) src.DeltaAsk,
                DealLimit = (double) src.DealLimit,
                PositionLimit = (double) src.PositionLimit
            };
        }
    }

    public class AccountAssetsPairsRepository : IAccountAssetPairsRepository
    {
        private readonly INoSQLTableStorage<AccountAssetPairEntity> _tableStorage;

        public AccountAssetsPairsRepository(INoSQLTableStorage<AccountAssetPairEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddOrReplaceAsync(IAccountAssetPair accountAssetPair)
        {
            await _tableStorage.InsertOrReplaceAsync(AccountAssetPairEntity.Create(accountAssetPair));
        }

        public async Task<IAccountAssetPair> GetAsync(string tradingConditionId, string baseAssetId, string assetPairId)
        {
            return await _tableStorage.GetDataAsync(AccountAssetPairEntity.GeneratePartitionKey(tradingConditionId, baseAssetId),
                AccountAssetPairEntity.GenerateRowKey(assetPairId));
        }

        public async Task<IEnumerable<IAccountAssetPair>> GetAllAsync(string tradingConditionId, string baseAssetId)
        {
            return await _tableStorage.GetDataAsync(AccountAssetPairEntity.GeneratePartitionKey(tradingConditionId, baseAssetId));
        }

        public async Task<IEnumerable<IAccountAssetPair>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }

        public async Task Remove(string tradingConditionId, string baseAssetId, string assetPairId)
        {
            await _tableStorage.DeleteAsync(
                AccountAssetPairEntity.GeneratePartitionKey(tradingConditionId, baseAssetId),
                AccountAssetPairEntity.GenerateRowKey(assetPairId));
        }

        public async Task<IEnumerable<IAccountAssetPair>> AddAssetPairs(string tradingConditionId, string baseAssetId,
            IEnumerable<string> assetPairsIds, AccountAssetsSettings defaults)
        {
            var entitiesToAdd = assetPairsIds.Select(x => AccountAssetPairEntity.Create(
                new AccountAssetPair
                {
                    BaseAssetId = baseAssetId,
                    TradingConditionId = tradingConditionId,
                    Instrument = x,
                    CommissionLong = defaults.CommissionLong,
                    CommissionLot = defaults.CommissionLot,
                    CommissionShort = defaults.CommissionShort,
                    DealLimit = defaults.DealLimit,
                    DeltaAsk = defaults.DeltaAsk,
                    DeltaBid = defaults.DeltaBid,
                    LeverageInit = defaults.LeverageInit,
                    LeverageMaintenance = defaults.LeverageMaintenance,
                    PositionLimit = defaults.PositionLimit,
                    SwapLong = defaults.SwapLong,
                    SwapShort = defaults.SwapShort,
                    OvernightSwapLong = defaults.OvernightSwapLong,
                    OvernightSwapShort = defaults.OvernightSwapShort
                })).ToArray();
            await _tableStorage.InsertAsync(entitiesToAdd);

            return entitiesToAdd;
        }
    }
}
