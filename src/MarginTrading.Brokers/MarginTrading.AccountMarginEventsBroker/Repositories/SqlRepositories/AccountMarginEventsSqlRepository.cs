using Common;
using Common.Log;
using Dapper;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Lykke.MarginTrading.BrokerBase.Extensions;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories
{
    internal class AccountMarginEventsSqlRepository : IAccountMarginEventsRepository
    {
        private const string TableName = "AccountMarginEvents";
        private const string CreateTableScript = "CREATE TABLE [{0}] (" +
            "[Id] [nvarchar](64) NOT NULL, " +
            "[EventId] [nvarchar](64) NOT NULL, " +
            "[AccountId] [nvarchar] (64) NOT NULL, " +
            "[TradingConditionId] [nvarchar] (64) NOT NULL, " +
            "[Balance] float NOT NULL, " +
            "[BaseAssetId] [nvarchar] (64) NOT NULL, " +
            "[EventTime] [datetime] NOT NULL, " +
            "[FreeMargin] float NOT NULL, " +
            "[IsEventStopout] [bit] NOT NULL, " +
            "[MarginAvailable] float NOT NULL, " +
            "[MarginCall] float NOT NULL, " +
            "[MarginInit] float NOT NULL, " +
            "[MarginUsageLevel] float NOT NULL, " +
            "[OpenPositionsCount] float NOT NULL, " +
            "[PnL] float NOT NULL, " +
            "[StopOut] float NOT NULL, " +
            "[TotalCapital] float NOT NULL, " +
            "[UsedMargin] float NOT NULL, " +
            "[WithdrawTransferLimit] float NOT NULL, " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly Settings _settings;
        private readonly ILog _log;

        private static readonly string GetColumns =
            string.Join(",", typeof(IAccountMarginEvent).GetProperties().Select(x => x.Name));

        private static readonly string GetFields =
            string.Join(",", typeof(IAccountMarginEvent).GetProperties().Select(x => "@" + x.Name));

        public AccountMarginEventsSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _settings = settings;
            using (var conn = new SqlConnection(_settings.Db.ConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountMarginEventsSqlRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
         }

        public async Task InsertOrReplaceAsync(IAccountMarginEvent report)
        {   

            using (var conn = new SqlConnection(_settings.Db.ConnString))
            {
                try
                {
                    await conn.ExecuteAsync(
                        $"insert into {TableName} ({GetColumns}) values ({GetFields})", report);
                }
                catch (Exception ex)
                {
                    var msg = $"Error {ex.Message} \n" +
                           "Entity <IAccountMarginEvent>: \n" +
                           report.ToJson();
                    await _log?.WriteWarningAsync(nameof(AccountMarginEventsSqlRepository), "InsertOrReplaceAsync",
                        null, msg);
                    throw new Exception(msg);
                }
            }
        }
    }
}
