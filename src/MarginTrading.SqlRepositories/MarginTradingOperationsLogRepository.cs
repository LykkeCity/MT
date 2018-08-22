using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Dapper;
using MarginTrading.Common.Services;
using MarginTrading.SqlRepositories.Entities;
using Microsoft.Extensions.Internal;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.SqlRepositories
{
    public class SqlOperationsLogRepository : IOperationsLogRepository
    {
        private readonly string _tableName;
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Id] [bigint] NOT NULL IDENTITY(1,1) PRIMARY KEY," +
                                                 "[DateTime] [DateTime] NULL," +
                                                 "[Name] [nvarchar] (MAX) NULL, " +
                                                 "[AccountId] [nvarchar] (64) NULL, " +
                                                 "[Input] [nvarchar] (MAX) NULL, " +
                                                 "[Data] [nvarchar] (MAX) NULL, " +
                                                 ");";
        
        private static Type DataType => typeof(OperationLogEntity);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));

        private readonly IDateService _dateService;
        private readonly string _connectionString;
        
        public SqlOperationsLogRepository(IDateService dateService, string logTableName, string connectionString)
        {
            _dateService = dateService;
            
            _tableName = logTableName;
            _connectionString = connectionString;
            
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.CreateTableIfDoesntExists(CreateTableScript, _tableName);
            }
        }

        public async Task AddLogAsync(IOperationLog log)
        {
            var logEntity = OperationLogEntity.Create(log, _dateService.Now());
            
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    $"insert into {_tableName} ({GetColumns}) values ({GetFields})", logEntity);
            }
        }
    }
}
