using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using JetBrains.Annotations;
using MarginTrading.Core;
using MarginTrading.Core.Messages;
using MarginTrading.Core.Settings;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAccountAssetEntity : TableEntity, IMarginTradingAccountAsset
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string Instrument => RowKey;
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

        public static string GeneratePartitionKey(string tradingConditionId, string baseAssetId)
        {
            return $"{tradingConditionId}_{baseAssetId}";
        }

        public static string GenerateRowKey(string instrument)
        {
            return instrument;
        }

        public static MarginTradingAccountAssetEntity Create(IMarginTradingAccountAsset src)
        {
            return new MarginTradingAccountAssetEntity
            {
                PartitionKey = GeneratePartitionKey(src.TradingConditionId, src.BaseAssetId),
                RowKey = GenerateRowKey(src.Instrument),
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
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

    public class MarginTradingAccountAssetsRepository : IMarginTradingAccountAssetRepository
    {
        private readonly INoSQLTableStorage<MarginTradingAccountAssetEntity> _tableStorage;

        public MarginTradingAccountAssetsRepository(INoSQLTableStorage<MarginTradingAccountAssetEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddOrReplaceAsync(IMarginTradingAccountAsset accountAsset)
        {
            await _tableStorage.InsertOrReplaceAsync(MarginTradingAccountAssetEntity.Create(accountAsset));
        }

        public async Task<IMarginTradingAccountAsset> GetAsync(string tradingConditionId, string baseAssetId, string assetPairId)
        {
            MarginTradingAccountAssetEntity entity = await _tableStorage.GetDataAsync(MarginTradingAccountAssetEntity.GeneratePartitionKey(tradingConditionId, baseAssetId),
                MarginTradingAccountAssetEntity.GenerateRowKey(assetPairId));

            return entity != null
                ? MarginTradingAccountAssetEntity.Create(entity)
                : null;
        }

        public async Task<IEnumerable<IMarginTradingAccountAsset>> GetAllAsync(string tradingConditionId, string baseAssetId)
        {
            return await _tableStorage.GetDataAsync(MarginTradingAccountAssetEntity.GeneratePartitionKey(tradingConditionId, baseAssetId));
        }

        public async Task<IEnumerable<IMarginTradingAccountAsset>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }

        public async Task AssignAssetPairs(string tradingConditionId, string baseAssetId, string[] assetPairsIds, AccountAssetsSettings defaults)
        {
            var currentInstruments = (await GetAllAsync(tradingConditionId, baseAssetId)).ToArray();

            if (currentInstruments.Any())
            {
                var toRemove = currentInstruments.Where(x => !assetPairsIds.Contains(x.Instrument)).Select(x => (MarginTradingAccountAssetEntity)x);

                foreach (var entity in toRemove)
                {
                    await _tableStorage.DeleteAsync(entity.PartitionKey, entity.RowKey);
                }
            }

            if (assetPairsIds.Any())
            {
                var toAdd = assetPairsIds.Where(x => !currentInstruments.Select(y => y.Instrument).Contains(x));
                var entitiesToAdd = toAdd.Select(x => MarginTradingAccountAssetEntity.Create(
                    new MarginTradingAccountAsset
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

        [ItemCanBeNull]
        public async Task<IMarginTradingAccountAsset> GetAccountAsset(string tradingConditionId,
            string accountAssetId, string assetPairId)
        {
            return (await GetAllAsync(tradingConditionId, accountAssetId)).FirstOrDefault(i => i.Instrument == assetPairId);
        }
    }
}
