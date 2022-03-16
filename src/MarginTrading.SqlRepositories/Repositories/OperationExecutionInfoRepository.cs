﻿// Copyright (c) 2021 Lykke Corp.
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

        private readonly ILog _log;
        private readonly IDateService _dateService;
        private readonly StoredProcedure _getPositionsInSpecialLiquidation = new StoredProcedure("getPositionsInSpecialLiquidation", "dbo");

        public OperationExecutionInfoRepository(
            string connectionString,
            ILog log,
            IDateService dateService) : base(connectionString)
        {
            _log = log;
            _dateService = dateService;

            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.CreateTableIfDoesntExists(CreateTableScript, TableName);
                }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(OperationExecutionInfoRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }

            ExecCreateOrAlter(_getPositionsInSpecialLiquidation.FileName);
        }

        public async Task<IOperationExecutionInfo<TData>> GetOrAddAsync<TData>(
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

                        return Convert<TData>(entity);
                    }

                    return Convert<TData>(operationInfo);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OperationExecutionInfoRepository), nameof(GetOrAddAsync), ex);
                throw;
            }
        }
        
        public async Task<PaginatedResponse<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>>> GetRfqAsync(string rfqId, string instrumentId,
            string accountId, List<SpecialLiquidationOperationState> states, DateTime? from, DateTime? to, int skip, int take,
            bool isAscendingOrder = false)
        {
            const string whereRfq = "WHERE i.OperationName = 'SpecialLiquidation'";

            var whereFields = (string.IsNullOrWhiteSpace(rfqId) ? "" : " AND i.Id = @rfqId")
                            + (from == null ? "" : " AND i.LastModified >= @from")
                            + (to == null ? "" : " AND i.LastModified < @to");

            var whereJson = (string.IsNullOrWhiteSpace(instrumentId) ? "" : " AND JSON_VALUE(i.Data, '$.Instrument') = @instrumentId")
                          + (string.IsNullOrWhiteSpace(accountId) ? "" : " AND JSON_VALUE(i.Data, '$.AccountId') = @accountId")
                          + (states == null || states.Count == 0 ? "" : $" AND JSON_VALUE(i.Data, '$.State') in ({string.Join(",", states.Select(x => $"'{x}'"))})");

            var whereClause = whereRfq + whereFields + whereJson;

            const int MaxResults = 100;
            var sorting = isAscendingOrder ? "ASC" : "DESC";
            take = take <= 0 ? MaxResults : Math.Min(take, MaxResults);
            var paginationClause = $"ORDER BY i.LastModified {sorting} OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";

            using var conn = new SqlConnection(ConnectionString);
            var sql = $@"
SELECT i.*, pause.*, cancelledPause.CancellationSource as LatestCancellationSource 
FROM [{TableName}] i 
LEFT JOIN [{OperationExecutionPauseRepository.TableName}] pause 
ON (pause.OperationId = i.Id AND pause.OperationName = i.OperationName AND pause.State != 'Cancelled')
LEFT JOIN [{OperationExecutionPauseRepository.TableName}] cancelledPause
ON (cancelledPause.Oid = 
    (SELECT MAX(Oid) 
    FROM [{OperationExecutionPauseRepository.TableName}] 
    WHERE OperationId = i.Id AND OperationName = i.OperationName AND [State] = 'Cancelled')) 
{whereClause} {paginationClause}; 

SELECT COUNT(*) FROM [{TableName}] i {whereClause}";
            
            var gridReader = await conn.QueryMultipleAsync(sql, new { rfqId, instrumentId, accountId, from, to });

            var contents = (await gridReader.ReadAsync<OperationExecutionInfoWithPauseEntity>()).ToList();
            var totalCount = await gridReader.ReadSingleAsync<int>();

            return new PaginatedResponse<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>>(
                    contents: contents.Select(x=>Convert<SpecialLiquidationOperationData>(x)).ToArray(),
                    start: skip,
                    size: contents.Count,
                    totalSize: totalCount
                );
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
                    await _log.WriteErrorAsync(nameof(OperationExecutionInfoRepository), nameof(GetOrAddAsync), ex);
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
                operationName: entity.OperationName,
                id: entity.Id,
                lastModified: entity.LastModified,
                data: entity.Data is string dataStr
                    ? JsonConvert.DeserializeObject<TData>(dataStr)
                    : ((JToken)entity.Data).ToObject<TData>());

            // if there is information on pause
            if (entity.Source.HasValue)
            {
                result.Pause = new OperationExecutionPause
                {
                    Source = entity.Source.Value,
                    CancellationSource = entity.CancellationSource,
                    LatestCancellationSource = entity.LatestCancellationSource,
                    CreatedAt = entity.CreatedAt.Value,
                    EffectiveSince = entity.EffectiveSince,
                    State = entity.State.Value,
                    Initiator = entity.Initiator.Value,
                    CancelledAt = entity.CancelledAt,
                    CancellationEffectiveSince = entity.CancellationEffectiveSince,
                    CancellationInitiator = entity.CancellationInitiator
                };
            }
            
            return result;
        }

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
    }
}