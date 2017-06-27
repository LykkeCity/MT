using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
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

        public async Task<IMarginTradingAccountAsset> GetAsync(string tradingConditionId, string baseAssetId, string instrument)
        {
            MarginTradingAccountAssetEntity entity = await _tableStorage.GetDataAsync(MarginTradingAccountAssetEntity.GeneratePartitionKey(tradingConditionId, baseAssetId),
                MarginTradingAccountAssetEntity.GenerateRowKey(instrument));

            return entity != null
                ? MarginTradingAccountAssetEntity.Create(entity)
                : null;
        }

        public async Task<IEnumerable<IMarginTradingAccountAsset>> GetAllAsync(string tradingConditionId, string baseAssetId)
        {
            IEnumerable<MarginTradingAccountAssetEntity> entities = await _tableStorage.GetDataAsync(MarginTradingAccountAssetEntity.GeneratePartitionKey(tradingConditionId, baseAssetId));

            return entities.Select(MarginTradingAccountAssetEntity.Create);
        }

        public async Task<IEnumerable<IMarginTradingAccountAsset>> GetAllAsync()
        {
            var entity = await _tableStorage.GetDataAsync();

            return entity.Any()
                ? entity.Select(MarginTradingAccountAsset.Create)
                : new List<IMarginTradingAccountAsset>();
        }

        public async Task AssignInstruments(string tradingConditionId, string baseAssetId, string[] instruments)
        {
            var currentInstruments =
                (await GetAllAsync(tradingConditionId, baseAssetId)).ToArray();

            if (currentInstruments != null && currentInstruments.Any())
            {
                var toRemove = currentInstruments.Where(x => !instruments.Contains(x.Instrument)).Select(x => (MarginTradingAccountAssetEntity)x);

                foreach (var entity in toRemove)
                {
                    await _tableStorage.DeleteAsync(entity.PartitionKey, entity.RowKey);
                }
            }

            if (instruments.Any())
            {
                var toAdd = instruments.Where(x => !currentInstruments.Select(y => y.Instrument).Contains(x));
                var entitiesToAdd = toAdd.Select(x => MarginTradingAccountAssetEntity.Create(new MarginTradingAccountAsset
                {
                    BaseAssetId = baseAssetId,
                    TradingConditionId = tradingConditionId,
                    Instrument = x
                }));
                await _tableStorage.InsertAsync(entitiesToAdd);
            }
        }
    }
}
