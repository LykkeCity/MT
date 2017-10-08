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

        decimal IMarginTradingAccountStats.MarginCall => (decimal) MarginCall;
        public double MarginCall { get; set; }
        decimal IMarginTradingAccountStats.StopOut  => (decimal) StopOut;
        public double StopOut { get; set; }
        decimal IMarginTradingAccountStats.TotalCapital  => (decimal) TotalCapital;
        public double TotalCapital { get; set; }
        decimal IMarginTradingAccountStats.FreeMargin  => (decimal) FreeMargin;
        public double FreeMargin { get; set; }
        decimal IMarginTradingAccountStats.MarginAvailable  => (decimal) MarginAvailable;
        public double MarginAvailable { get; set; }
        decimal IMarginTradingAccountStats.UsedMargin  => (decimal) UsedMargin;
        public double UsedMargin { get; set; }
        decimal IMarginTradingAccountStats.MarginInit  => (decimal) MarginInit;
        public double MarginInit { get; set; }
        decimal IMarginTradingAccountStats.PnL  => (decimal) PnL;
        public double PnL { get; set; }
        decimal IMarginTradingAccountStats.OpenPositionsCount  => (decimal) OpenPositionsCount;
        public double OpenPositionsCount { get; set; }
        decimal IMarginTradingAccountStats.MarginUsageLevel  => (decimal) MarginUsageLevel;
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

    public interface IMarginTradingAccountStatsRepository
    {
        Task<IEnumerable<IMarginTradingAccountStats>> GetAllAsync();
        Task InsertOrReplaceBatchAsync(IEnumerable<MarginTradingAccountStatsEntity> stats);
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

        public Task InsertOrReplaceBatchAsync(IEnumerable<MarginTradingAccountStatsEntity> stats)
        {
            var tasks = BatchEntityInsertHelper.MakeBatchesByPartitionKey(stats)
                .Select(b => _tableStorage.InsertOrReplaceBatchAsync(b));
            return Task.WhenAll(tasks);
        }
    }
}