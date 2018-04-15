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
            "[OID] [int] NOT NULL IDENTITY (1,1) PRIMARY KEY," +
			"[Instrument] [nvarchar] (64) NOT NULL, " +
			"[Exchange] [nvarchar] (64) NOT NULL, " +
			"[BaseAsset] [nvarchar] (64) NOT NULL, " +
			"[QuoteAsset] [nvarchar] (64) NOT NULL, " +
			"[Type] [nvarchar] (64) NOT NULL, " +
            "[Time] [datetime] NOT NULL," +
            "[Price] [float] NOT NULL, " +
            "[Volume] [float] NOT NULL, " +
            "[Fee] [float] NOT NULL, " +
            "[Id] [nvarchar] (64) constraint ux_{0}_Id unique NONCLUSTERED NOT NULL, " +
            "[Status] [nvarchar] (64) NOT NULL, " +
            "[Message] [text] NOT NULL, " +
            ");";

        private static readonly string GetColumns =
            string.Join(",", typeof(IExternalOrderReport).GetProperties().Select(x => x.Name));

        private static readonly string GetFields =
            string.Join(",", typeof(IExternalOrderReport).GetProperties().Select(x => "@" + x.Name));

        private static readonly string GetUpdateClause = string.Join(",",
            typeof(IExternalOrderReport).GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly Settings.AppSettings _appSettings;
        private readonly ILog _log;

        public ExternalOrderReportSqlRepository(Settings.AppSettings appSettings, ILog log)
        {
            _log = log;
            _appSettings = appSettings;
            using (var conn = new SqlConnection(_appSettings.Db.ReportsSqlConnString))
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
            
            using (var conn = new SqlConnection(_appSettings.Db.ReportsSqlConnString))
            {
                try
                {
                    try
                    {
                        await conn.ExecuteAsync(
                            $"insert into {TableName} ({GetColumns}) values ({GetFields})", entity);
                    }
                    catch (SqlException)
                    {
                        await conn.ExecuteAsync(
                            $"update {TableName} set {GetUpdateClause} where Id=@Id", entity); 
                    }
                }
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
