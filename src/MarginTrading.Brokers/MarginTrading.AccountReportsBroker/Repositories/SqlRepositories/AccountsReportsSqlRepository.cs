using Common.Log;
using Dapper;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker.Repositories.SqlRepositories
{
    public class AccountsReportsSqlRepository : IAccountsReportsRepository
    {
        private const string TableName = "ClientAccountsReports";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
            "[Id] [nvarchar](64) NOT NULL, " +
            "[Date] [datetime] NOT NULL, " +
            "[TakerCounterpartyId] [nvarchar] (64) NOT NULL, " +
            "[TakerAccountId] [nvarchar] (64) NOT NULL, " +
            "[BaseAssetId] [nvarchar] (64) NOT NULL, " +
            "[IsLive] [bit] NOT NULL, " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly Settings _settings;
        private readonly ILog _log;

        public AccountsReportsSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _settings = settings;
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log.WriteErrorAsync("AccountsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task InsertOrReplaceAsync(IAccountsReport report)
        {
            string query = $"insert into {TableName} " +
               "(Id, Date, TakerCounterpartyId, TakerAccountId, BaseAssetId, IsLive) " +
               " values " +
               "(@Id, @Date, @TakerCounterpartyId, @TakerAccountId, @BaseAssetId, @IsLive)";

            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { await conn.ExecuteAsync(query, report); }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync("AccountsReportsSqlRepository", "InsertOrReplaceAsync", null, ex);
                    throw;
                }
            }
        }
    }
}
