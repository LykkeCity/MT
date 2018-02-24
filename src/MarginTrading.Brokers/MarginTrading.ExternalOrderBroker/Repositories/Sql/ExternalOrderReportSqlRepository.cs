using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using MarginTrading.BrokerBase;
using MarginTrading.ExternalOrderBroker.Models;
using MarginTrading.ExternalOrderBroker.Repositories.Azure;

namespace MarginTrading.ExternalOrderBroker.Repositories.Sql
{
    public class ExternalOrderReportSqlRepository : IExternalOrderReportRepository
    {
        private const string TableName = "ExternalOrderReport";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
            "[AccountAssetId] [nvarchar](64) NOT NULL, " +
			"[Instrument] [nvarchar] (64) NOT NULL, " +
			"[Exchange] [nvarchar] (64) NOT NULL, " +
			"[BaseAsset] [nvarchar] (64) NOT NULL, " +
			"[QuoteAsset] [nvarchar] (64) NOT NULL, " +
			"[Type] [nvarchar] (64) NOT NULL, " +
            "[Time] [datetime] NOT NULL," +
            "[Price] float NOT NULL, " +
            "[Volume] float NOT NULL, " +
            "[Fee] float NOT NULL, " +
            "[Id] [nvarchar] (64) NOT NULL, " +
            "[Status] [nvarchar] (64) NOT NULL, " +
            "[Message] [text] NOT NULL, " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private string GetColumns =>
            string.Join(",", typeof(IExternalOrderReport).GetProperties().Select(x => x.Name));

        private string GetFields =>
            string.Join(",", typeof(IExternalOrderReport).GetProperties().Select(x => "@" + x.Name));

        private string GetUpdateClause => string.Join(",",
            typeof(IExternalOrderReport).GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly Settings _settings;
        private readonly ILog _log;

        public ExternalOrderReportSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _settings = settings;
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync("ExternalOrderReportSqlRepository", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task InsertOrReplaceAsync(IExternalOrderReport obj)
        {
            var entity = ExternalOrderReportEntity.Create(obj);
            
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                var res = conn.ExecuteScalar($"select Id from {TableName} where Id = '{entity.Id}'");
                var query = res == null
                    ? $"insert into {TableName} " + $"({GetColumns})" + " values " + $"({GetFields})"
                    : $"update {TableName} set " + $"{GetUpdateClause}" + " where Id=@Id ";
               
                try { await conn.ExecuteAsync(query, entity); }
                catch (Exception ex)
                {
                    var msg = $"Error {ex.Message} \n" +
                           "Entity <ExternalOrderReportEntity>: \n" +
                           entity.ToJson();
                    await _log?.WriteWarningAsync("ExternalOrderReportSqlRepository", "InsertOrReplaceAsync", null, msg);
                    throw new Exception(msg);
                }
            }
        }
    }
}
