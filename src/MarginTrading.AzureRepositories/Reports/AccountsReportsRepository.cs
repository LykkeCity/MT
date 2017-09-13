using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Helpers;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories.Reports
{
    public class AccountsStatReport : TableEntity
    {
        public string BaseAssetId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string AccountId
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public double Balance { get; set; }
        public double WithdrawTransferLimit { get; set; }
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
        public bool IsLive { get; set; }
    }

    public interface IAccountsStatsReportsRepository
    {
        Task InsertOrReplaceBatchAsync(IEnumerable<AccountsStatReport> stats);
    }

    public class AccountsStatsReportsRepository : IAccountsStatsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountsStatReport> _tableStorage;

        public AccountsStatsReportsRepository(INoSQLTableStorage<AccountsStatReport> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task InsertOrReplaceBatchAsync(IEnumerable<AccountsStatReport> stats)
        {
            var tasks = BatchEntityInsertHelper.MakeBatchesByPartitionKey(stats)
                .Select(b => _tableStorage.InsertOrReplaceBatchAsync(b));
            return Task.WhenAll(tasks);
        }
    }
}
