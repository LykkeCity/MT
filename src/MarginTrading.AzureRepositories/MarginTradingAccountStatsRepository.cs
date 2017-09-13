using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Helpers;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAccountStatsEntity : TableEntity, IMarginTradingAccountStats
    {
        public string AccountId
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string BaseAssetId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public double MarginCall { get; set; }
        public double StopOut { get; set; }
        public double TotalCapital { get; set; }
        public double FreeMargin { get; set; }
        public double MarginAvailable { get; set; }
        public double UsedMargin { get; set; }
        public double MarginInit { get; set; }
        public double PnL { get; set; }
        public double OpenPositionsCount { get; set; }
        public double MarginUsageLevel { get; set; }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return new Dictionary<string, EntityProperty>
            {
                {nameof(AccountId), new EntityProperty(AccountId)},
                {nameof(BaseAssetId), new EntityProperty(BaseAssetId)},
                {nameof(MarginCall), new EntityProperty(MarginCall)},
                {nameof(StopOut), new EntityProperty(StopOut)},
                {nameof(TotalCapital), new EntityProperty(TotalCapital)},
                {nameof(FreeMargin), new EntityProperty(FreeMargin)},
                {nameof(MarginAvailable), new EntityProperty(MarginAvailable)},
                {nameof(UsedMargin), new EntityProperty(UsedMargin)},
                {nameof(MarginInit), new EntityProperty(MarginInit)},
                {nameof(PnL), new EntityProperty(PnL)},
                {nameof(OpenPositionsCount), new EntityProperty(OpenPositionsCount)},
                {nameof(MarginUsageLevel), new EntityProperty(MarginUsageLevel)},
            };
        }
    }

    public class MarginTradingAccountStatsRepository : IMarginTradingAccountStatsRepository
    {
        private readonly INoSQLTableStorage<MarginTradingAccountStatsEntity> _tableStorage;

        public MarginTradingAccountStatsRepository(INoSQLTableStorage<MarginTradingAccountStatsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IMarginTradingAccountStats>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }

        public Task InsertOrReplaceBatchAsync(IEnumerable<IMarginTradingAccountStats> stats)
        {
            var entities = stats.Select(item => new MarginTradingAccountStatsEntity
            {
                AccountId = item.AccountId,
                BaseAssetId = item.BaseAssetId,
                MarginCall = item.MarginCall,
                StopOut = item.StopOut,
                TotalCapital = item.TotalCapital,
                FreeMargin = item.FreeMargin,
                MarginAvailable = item.MarginAvailable,
                UsedMargin = item.UsedMargin,
                MarginInit = item.MarginInit,
                PnL = item.PnL,
                OpenPositionsCount = item.OpenPositionsCount,
                MarginUsageLevel = item.MarginUsageLevel,
            });

            var tasks = BatchEntityInsertHelper.MakeBatchesByPartitionKey(entities)
                .Select(b => _tableStorage.InsertOrReplaceBatchAsync(b));
            return Task.WhenAll(tasks);
        }
    }
}