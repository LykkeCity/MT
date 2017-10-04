using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
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
        decimal IAccountAssetPair.SwapLongPct => (decimal) SwapLongPct;
        public double SwapLongPct { get; set; }
        decimal IAccountAssetPair.SwapShortPct => (decimal) SwapShortPct;
        public double SwapShortPct { get; set; }
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
                SwapLongPct = (double) src.SwapLongPct,
                SwapShortPct = (double) src.SwapShortPct,
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

        public async Task AssignAssetPairs(string tradingConditionId, string baseAssetId, string[] assetPairsIds, AccountAssetsSettings defaults)
        {
            var currentInstruments = (await GetAllAsync(tradingConditionId, baseAssetId)).ToArray();

            if (currentInstruments.Any())
            {
                var toRemove = currentInstruments.Where(x => !assetPairsIds.Contains(x.Instrument)).Select(x => (AccountAssetPairEntity)x);

                foreach (var entity in toRemove)
                {
                    await _tableStorage.DeleteAsync(entity.PartitionKey, entity.RowKey);
                }
            }

            if (assetPairsIds.Any())
            {
                var toAdd = assetPairsIds.Where(x => !currentInstruments.Select(y => y.Instrument).Contains(x));
                var entitiesToAdd = toAdd.Select(x => AccountAssetPairEntity.Create(
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
                        SwapLongPct = defaults.SwapLongPct,
                        SwapShort = defaults.SwapShort,
                        SwapShortPct = defaults.SwapShortPct
                    }));
                await _tableStorage.InsertAsync(entitiesToAdd);
            }
        }
    }
}
