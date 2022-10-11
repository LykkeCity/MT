// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Common;
using Common.Log;
using Dapper;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;
using MarginTrading.SqlRepositories.Entities;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Model;
using MarginTrading.Backend.Core.Rfq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class OperationExecutionInfoRepository : SqlRepositoryBase, IOperationExecutionInfoRepository
    {
        private const string TableName = "MarginTradingExecutionInfo";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Oid] [bigint] NOT NULL IDENTITY(1,1) PRIMARY KEY," +
                                                 "[Id] [nvarchar] (128) NOT NULL," +
                                                 "[LastModified] [datetime] NOT NULL, " +
                                                 "[OperationName] [nvarchar] (64) NULL, " +
                                                 "[Version] [nvarchar] (64) NULL, " +
                                                 "[Data] [nvarchar] (MAX) NOT NULL," +
                                                 "CONSTRAINT [MTEx_Id] UNIQUE NONCLUSTERED ([Id], [OperationName])" +
                                                 ");";

        private static Type DataType => typeof(IOperationExecutionInfo<object>);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",",
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly ILogger<OperationExecutionInfoRepository> _logger;
        private readonly IDateService _dateService;
        private readonly StoredProcedure _getPositionsInSpecialLiquidation = new StoredProcedure("getPositionsInSpecialLiquidation", "dbo");
        private readonly StoredProcedure _getRfqExecutionInfoWithPause = new StoredProcedure("getRfqExecutionInfoWithPause", "dbo");

        public OperationExecutionInfoRepository(
            string connectionString,
            ILogger<OperationExecutionInfoRepository> logger,
            IDateService dateService) : base(connectionString, logger)
        {
            _logger = logger;
            _dateService = dateService;

            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.CreateTableIfDoesntExists(CreateTableScript, TableName);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"{nameof(OperationExecutionInfoRepository)}, CreateTableIfDoesntExists\r\n " +
                                      $"Exception Message: {ex.Message}\r\n" +
                                      $"Stack Trace: {ex.StackTrace}\r\n" +
                                      $"Timestamp UTC: {DateTime.UtcNow}");
                    if (ex.InnerException != null)
                    {
                        _logger?.LogError($"{nameof(OperationExecutionInfoRepository)}, CreateTableIfDoesntExists\r\n " +
                                          $"Exception Message: {ex.InnerException.Message}\r\n" +
                                          $"Stack Trace: {ex.InnerException.StackTrace}\r\n" +
                                          $"Timestamp UTC: {DateTime.UtcNow}");
                    }
                    throw;
                }
            }

            ExecCreateOrAlter(_getPositionsInSpecialLiquidation);
            ExecCreateOrAlter(_getRfqExecutionInfoWithPause);
        }

        public async Task<(IOperationExecutionInfo<TData>, bool added)> GetOrAddAsync<TData>(
            string operationName, string operationId, Func<IOperationExecutionInfo<TData>> factory) where TData : class
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    var operationInfo = await conn.QueryFirstOrDefaultAsync<OperationExecutionInfoEntity>(
                        $"SELECT * FROM {TableName} WHERE Id=@operationId and OperationName=@operationName",
                        new { operationId, operationName });

                    if (operationInfo == null)
                    {
                        var entity = Convert(factory(), _dateService.Now());

                        await conn.ExecuteAsync(
                            $"insert into {TableName} ({GetColumns}) values ({GetFields})", entity);

                        return (Convert<TData>(entity), true);
                    }

                    return (Convert<TData>(operationInfo), false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"{nameof(OperationExecutionInfoRepository)}, {nameof(GetOrAddAsync)}\r\n " +
                                  $"Exception Message: {ex.Message}\r\n" +
                                  $"Stack Trace: {ex.StackTrace}\r\n" +
                                  $"Timestamp UTC: {DateTime.UtcNow}");
                if (ex.InnerException != null)
                {
                    _logger?.LogError($"{nameof(OperationExecutionInfoRepository)}, {nameof(GetOrAddAsync)}\r\n " +
                                      $"Exception Message: {ex.InnerException.Message}\r\n" +
                                      $"Stack Trace: {ex.InnerException.StackTrace}\r\n" +
                                      $"Timestamp UTC: {DateTime.UtcNow}");
                }
                throw;
            }
        }

        public async Task<IOperationExecutionInfo<TData>> GetAsync<TData>(string operationName, string id) where TData : class
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                var operationInfo = await conn.QuerySingleOrDefaultAsync<OperationExecutionInfoEntity>(
                    $"SELECT * FROM {TableName} WHERE Id = @id and OperationName=@operationName",
                    new { id, operationName });

                return operationInfo == null ? null : Convert<TData>(operationInfo);
            }
        }

        public async Task<PaginatedResponse<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>>>
            GetRfqAsync(int skip,
                int take,
                string id = null,
                string instrumentId = null,
                string accountId = null,
                List<SpecialLiquidationOperationState> states = null,
                DateTime? @from = null,
                DateTime? to = null)
        {
            var entities = (await GetAllAsync(
                    _getRfqExecutionInfoWithPause.FullyQualifiedName,
                    BuildSqlParameters(),
                    MapRfqExecutionInfoWithPause))
                .ToList();

            var contents = entities
                .Select(Convert<SpecialLiquidationOperationData>)
                .ToList();

            return new PaginatedResponse<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>>(
                contents: contents,
                start: skip,
                size: contents.Count,
                totalSize: entities.FirstOrDefault()?.TotalCount ?? 0
            );

            SqlParameter[] BuildSqlParameters()
            {
                var sqlParams = new List<SqlParameter>();

                var stateNamesCollection = new SpecialLiquidationStateNamesCollection();
                if (states?.Any() ?? false)
                {
                    stateNamesCollection.AddRange(states.Select(x => new StateName { Name = x.ToString() }));
                }

                sqlParams.Add(new SqlParameter
                {
                    ParameterName = "@states",
                    SqlDbType = SqlDbType.Structured,
                    TypeName = "dbo.SpecialLiquidationStateListDataType",
                    Value = stateNamesCollection.Count > 0 ? stateNamesCollection : null
                });

                sqlParams.Add(new SqlParameter
                {
                    ParameterName = "@skip",
                    SqlDbType = SqlDbType.Int,
                    Value = skip
                });

                sqlParams.Add(new SqlParameter
                {
                    ParameterName = "@take",
                    SqlDbType = SqlDbType.Int,
                    Value = take
                });

                if (id != null)
                {
                    sqlParams.Add(new SqlParameter
                    {
                        ParameterName = "@id",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = id
                    });
                }

                if (instrumentId != null)
                {
                    sqlParams.Add(new SqlParameter
                    {
                        ParameterName = "@instrumentId",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = instrumentId
                    });
                }

                if (accountId != null)
                {
                    sqlParams.Add(new SqlParameter
                    {
                        ParameterName = "@accountId",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = accountId
                    });
                }

                if (@from != null)
                {
                    sqlParams.Add(new SqlParameter
                    {
                        ParameterName = "@from",
                        SqlDbType = SqlDbType.DateTime,
                        Value = @from
                    });
                }

                if (to != null)
                {
                    sqlParams.Add(new SqlParameter
                    {
                        ParameterName = "@to",
                        SqlDbType = SqlDbType.DateTime,
                        Value = to
                    });
                }

                return sqlParams.ToArray();
            }
        }

        public async Task Save<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            var entity = Convert(executionInfo, _dateService.Now());
            var affectedRows = 0;

            using (var conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    affectedRows = await conn.ExecuteAsync(
                        $"update {TableName} set {GetUpdateClause} where Id=@Id " +
                                                                        "and OperationName=@OperationName " +
                                                                        "and LastModified=@PrevLastModified",
                        entity);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"{nameof(OperationExecutionInfoRepository)}, {nameof(GetOrAddAsync)}\r\n " +
                                      $"Exception Message: {ex.Message}\r\n" +
                                      $"Stack Trace: {ex.StackTrace}\r\n" +
                                      $"Timestamp UTC: {DateTime.UtcNow}");
                    if (ex.InnerException != null)
                    {
                        _logger?.LogError($"{nameof(OperationExecutionInfoRepository)}, {nameof(GetOrAddAsync)}\r\n " +
                                          $"Exception Message: {ex.InnerException.Message}\r\n" +
                                          $"Stack Trace: {ex.InnerException.StackTrace}\r\n" +
                                          $"Timestamp UTC: {DateTime.UtcNow}");
                    }
                    throw;
                }
            }

            if (affectedRows == 0)
            {
                var existingExecutionInfo = await GetAsync<TData>(executionInfo.OperationName, executionInfo.Id);

                if (existingExecutionInfo == null)
                {
                    throw new InvalidOperationException(
                        $"Execution info {executionInfo.OperationName}:{executionInfo.Id} does not exist");
                }

                throw new InvalidOperationException(
                    $"Optimistic Concurrency Violation Encountered. " +
                    $"Existing info: [{existingExecutionInfo.ToJson()}] " +
                    $"New info: [{executionInfo.ToJson()}]");
            }
        }

        public Task<IEnumerable<string>> FilterPositionsInSpecialLiquidationAsync(IEnumerable<string> positionIds)
        {
            var positionIdCollection = new PositionIdCollection();
            positionIdCollection.AddRange(positionIds.Select(id => new PositionId {Id = id}));
            
            return GetAllAsync(
                _getPositionsInSpecialLiquidation.FullyQualifiedName,
                new[]
                {
                    new SqlParameter
                    {
                        ParameterName = "@positions",
                        SqlDbType = SqlDbType.Structured,
                        TypeName = "dbo.PositionListDataType",
                        Value = positionIdCollection.Count > 0 ? positionIdCollection : null
                    }
                }, MapPositionId);
        }

        private static OperationExecutionInfoWithPause<TData> Convert<TData>(OperationExecutionInfoWithPauseEntity entity)
            where TData : class
        {
            var result = new OperationExecutionInfoWithPause<TData>(
                entity.ExecutionInfo.OperationName,
                entity.ExecutionInfo.Id, 
                entity.ExecutionInfo.LastModified,
                entity.ExecutionInfo.Data is string dataStr
                    ? JsonConvert.DeserializeObject<TData>(dataStr)
                    : ((JToken)entity.ExecutionInfo.Data).ToObject<TData>());
            
            if (entity.CurrentPause != null)
            {
                result.CurrentPause = Convert(entity.CurrentPause);
            }

            if (entity.LatestCancelledPause != null)
            {
                result.LatestCancelledPause = Convert(entity.LatestCancelledPause);
            }
            
            return result;
        }

        private static OperationExecutionPause Convert(OperationExecutionPauseEntity entity) =>
            new OperationExecutionPause
            {
                Source = entity.Source,
                CancellationSource = entity.CancellationSource,
                CreatedAt = entity.CreatedAt,
                EffectiveSince = entity.EffectiveSince,
                CancellationEffectiveSince = entity.CancellationEffectiveSince,
                Initiator = entity.Initiator,
                CancellationInitiator = entity.CancellationInitiator,
                CancelledAt = entity.CancelledAt,
                State = entity.State
            };

        private static OperationExecutionInfo<TData> Convert<TData>(OperationExecutionInfoEntity entity)
            where TData : class
        {
            return new OperationExecutionInfo<TData>(
                operationName: entity.OperationName,
                id: entity.Id,
                lastModified: entity.LastModified,
                data: entity.Data is string dataStr
                    ? JsonConvert.DeserializeObject<TData>(dataStr)
                    : ((JToken)entity.Data).ToObject<TData>());
        }

        private static OperationExecutionInfoEntity Convert<TData>(IOperationExecutionInfo<TData> model, DateTime now)
            where TData : class
        {
            return new OperationExecutionInfoEntity
            {
                Id = model.Id,
                OperationName = model.OperationName,
                Data = model.Data.ToJson(),
                PrevLastModified = model.LastModified,
                LastModified = now
            };
        }
        
        private static string MapPositionId(SqlDataReader reader)
        {
            return reader?["PositionId"] as string;
        }

        private static OperationExecutionInfoWithPauseEntity MapRfqExecutionInfoWithPause(SqlDataReader reader)
        {
            var currenPauseOid = reader["currentPauseOid"] as long?;
            var latestCancelledPauseOid = reader["latestCancelledPauseOid"] as long?;

            return new OperationExecutionInfoWithPauseEntity
            {
                ExecutionInfo = new OperationExecutionInfoEntity
                {
                    Id = reader["Id"] as string,
                    Data = reader["Data"] as string,
                    LastModified = (reader["LastModified"] as DateTime?).GetValueOrDefault(),
                    OperationName = "SpecialLiquidation"
                },

                CurrentPause = currenPauseOid.HasValue
                    ? new OperationExecutionPauseEntity
                    {
                        Oid = currenPauseOid,
                        Source = Enum.Parse<PauseSource>(reader["currentPauseSource"] as string ?? throw new ArgumentOutOfRangeException(message: @$"Current pause source value is empty. Oid = {reader["currentPauseOid"]}", null)),
                        CreatedAt = (reader["currentPauseCreatedAt"] as DateTime?).GetValueOrDefault(),
                        EffectiveSince = reader["currentPauseEffectiveSince"] as DateTime?,
                        State = Enum.Parse<PauseState>(reader["currentPauseState"] as string ?? throw new ArgumentOutOfRangeException(message: @$"Current pause state value is empty. Oid = {reader["currentPauseOid"]}", null)),
                        Initiator = reader["currentPauseInitiator"] as string
                    }
                    : null,

                LatestCancelledPause = latestCancelledPauseOid.HasValue
                    ? new OperationExecutionPauseEntity
                    {
                        Oid = latestCancelledPauseOid,
                        Source = Enum.Parse<PauseSource>(reader["latestCancelledPauseSource"] as string ?? throw new ArgumentOutOfRangeException(message: @$"Latest cancelled pause source value is empty. Oid = {reader["latestCancelledPauseOid"]}", null)),
                        CancellationSource = Enum.Parse<PauseCancellationSource>(reader["latestCancelledPauseCancellationSource"] as string ?? throw new ArgumentOutOfRangeException(message: @$"Latest cancelled pause cancellation source value is empty. Oid = {reader["latestCancelledPauseOid"]}", null)),
                        CreatedAt = (reader["latestCancelledPauseCreatedAt"] as DateTime?).GetValueOrDefault(),
                        EffectiveSince = reader["latestCancelledPauseEffectiveSince"] as DateTime?,
                        State = Enum.Parse<PauseState>(reader["latestCancelledPauseState"] as string ?? throw new ArgumentOutOfRangeException(message: @$"Latest cancelled pause state value is empty. Oid = {reader["latestCancelledPauseOid"]}", null)),
                        Initiator = reader["latestCancelledPauseInitiator"] as string,
                        CancelledAt = reader["latestCancelledPauseCancelledAt"] as DateTime?,
                        CancellationEffectiveSince = reader["latestCancelledPauseCancellationEffectiveSince"] as DateTime?,
                        CancellationInitiator = reader["latestCancelledPauseCancellationInitiator"] as string
                    }
                : null,

                TotalCount = (reader["TotalCount"] as int?).GetValueOrDefault()
            };
        }
    }
}