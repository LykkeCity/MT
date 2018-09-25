using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Dapper;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Services;
using MarginTrading.SqlRepositories.Entities;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class AccountMarginFreezingRepository : IAccountMarginFreezingRepository
    {
        private const string TableName = "AccountMarginFreezing";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[OperationId] [nvarchar] (128) NOT NULL PRIMARY KEY," +
                                                 "[AccountId] [nvarchar] (64) NOT NULL, " +
                                                 "[Amount] decimal (24, 12) NOT NULL " +
                                                 ");";
        
        private static Type DataType => typeof(IAccountMarginFreezing);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",",
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly IConvertService _convertService;
        private readonly MarginTradingSettings _settings;
        private readonly ILog _log;

        /// <summary>
        /// For testing purposes
        /// </summary>
        public AccountMarginFreezingRepository(){}
        
        public AccountMarginFreezingRepository(IConvertService convertService, MarginTradingSettings settings, ILog log)
        {
            _convertService = convertService;
            _log = log;
            _settings = settings;
            
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountMarginFreezingRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }
        
        public async Task<IReadOnlyList<IAccountMarginFreezing>> GetAllAsync()
        {
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                var entities = await conn.QueryAsync<AccountMarginFreezingEntity>(
                    $"SELECT * FROM {TableName}");
                
                return entities.ToList();
            }
        }

        public async Task<bool> TryInsertAsync(IAccountMarginFreezing item)
        {
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                try
                {
                    await conn.ExecuteAsync(
                        $"insert into {TableName} ({GetColumns}) values ({GetFields})",
                        _convertService.Convert<AccountMarginFreezingEntity>(item));
                    return true;
                }
                catch (Exception ex)
                {
                    await _log.WriteWarningAsync(nameof(AccountMarginFreezingRepository), nameof(TryInsertAsync),
                        $"Failed to insert account margin freezing item id {item.OperationId}", ex);
                    return false;
                }
            }
        }

        public async Task DeleteAsync(string operationId)
        {
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                await conn.ExecuteAsync($"DELETE {TableName} WHERE OperationId=@operationId", new {operationId});
            }
        }
    }
}