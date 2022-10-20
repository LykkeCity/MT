// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using Lykke.Snow.Common;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.SqlRepositories.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class OperationExecutionPauseRepository : SqlRepositoryBase, IOperationExecutionPauseRepository
    {
        private const string CreateTableScript = @"
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE [name] = '{0}' AND schema_id = schema_id('dbo'))
BEGIN
    CREATE TABLE [dbo].[{0}]
    (
        Oid                        bigint unique identity(1, 1)    not null,
        OperationId                nvarchar(128)                   not null,
        OperationName              nvarchar(64),
        Source                     nvarchar(64)                    not null,
        CancellationSource         nvarchar(64),
        CreatedAt                  datetime2                       not null,
        EffectiveSince             datetime2,
        State                      nvarchar(32) default 'Pending'  not null,
        Initiator                  nvarchar(64)                    not null,
        CancelledAt                datetime2,
        CancellationEffectiveSince datetime2,
        CancellationInitiator      nvarchar(64),
        primary key (Oid),
        foreign key (OperationId, OperationName) references [dbo].[MarginTradingExecutionInfo] (Id, OperationName)
    );
END";
        
        private readonly ILogger<OperationExecutionPauseRepository> _logger;
        
        public const string TableName = "MarginTradingExecutionPause";

        static OperationExecutionPauseRepository()
        {
            SqlMapper.AddTypeMap(typeof(Initiator), DbType.String);
        }

        public OperationExecutionPauseRepository(
            string connectionString,
            ILogger<OperationExecutionPauseRepository> logger) : base(connectionString, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Execute(string.Format(CreateTableScript, TableName));
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(CreateTableScript)}\r\n " +
                                      $"Exception Message: {ex.Message}\r\n" +
                                      $"Stack Trace: {ex.StackTrace}\r\n" +
                                      $"Timestamp UTC: {DateTime.UtcNow}");
                    if (ex.InnerException != null)
                    {
                        _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(CreateTableScript)}\r\n " +
                                          $"Exception Message: {ex.InnerException.Message}\r\n" +
                                          $"Stack Trace: {ex.InnerException.StackTrace}\r\n" +
                                          $"Timestamp UTC: {DateTime.UtcNow}");
                    }
                    throw;
                }
            }
        }

        public async Task AddAsync(Pause pause)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.ExecuteAsync(@$"
insert into [dbo].[{TableName}] (OperationId, OperationName, Source, CreatedAt, State, Initiator)
values (@OperationId, @OperationName, @Source, @CreatedAt, @State, @Initiator)", PauseEntity.Create(pause));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(AddAsync)}\r\n " +
                                  $"Exception Message: {ex.Message}\r\n" +
                                  $"Stack Trace: {ex.StackTrace}\r\n" +
                                  $"Timestamp UTC: {DateTime.UtcNow}");
                if (ex.InnerException != null)
                {
                    _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(AddAsync)}\r\n " +
                                      $"Exception Message: {ex.InnerException.Message}\r\n" +
                                      $"Stack Trace: {ex.InnerException.StackTrace}\r\n" +
                                      $"Timestamp UTC: {DateTime.UtcNow}");
                }
                throw;
            }

            _logger.LogInformation($"{nameof(OperationExecutionPauseRepository)}, {nameof(AddAsync)}\r\n " +
                                   $"Pause for operation with id [{pause.OperationId}] and name [{pause.OperationName}] has been successfully persisted");
        }

        public async Task<bool> UpdateAsync(long oid,
            DateTime? effectiveSince,
            PauseState state,
            DateTime? cancelledAt,
            DateTime? cancellationEffectiveSince,
            Initiator cancellationInitiator,
            PauseCancellationSource? cancellationSource)
        {
            int affectedRows = 0;
            
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    affectedRows = await conn.ExecuteAsync($@"
update [dbo].[{TableName}] 
set EffectiveSince = @EffectiveSince, 
    State = @State, 
    CancelledAt = @CancelledAt, 
    CancellationEffectiveSince = @CancellationEffectiveSince, 
    CancellationInitiator = @CancellationInitiator, 
    CancellationSource = @CancellationSource 
where Oid = @Oid", new
                    {
                        EffectiveSince = effectiveSince,
                        State = state.ToString(),
                        CancelledAt = cancelledAt,
                        CancellationEffectiveSince = cancellationEffectiveSince,
                        CancellationInitiator = cancellationInitiator,
                        CancellationSource = cancellationSource?.ToString(),
                        Oid = oid
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(UpdateAsync)}\r\n " +
                                  $"Exception Message: {ex.Message}\r\n" +
                                  $"Stack Trace: {ex.StackTrace}\r\n" +
                                  $"Timestamp UTC: {DateTime.UtcNow}");
                if (ex.InnerException != null)
                {
                    _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(UpdateAsync)}\r\n " +
                                      $"Exception Message: {ex.InnerException.Message}\r\n" +
                                      $"Stack Trace: {ex.InnerException.StackTrace}\r\n" +
                                      $"Timestamp UTC: {DateTime.UtcNow}");
                }
                throw;
            }

            if (affectedRows == 0)
            {
                _logger?.LogWarning($"{nameof(OperationExecutionPauseRepository)}, {nameof(UpdateAsync)}: Pause with oid [{oid}] has not been updated. Probably, it was not found");
                return false;
            }

            var json = new
            {
                effectiveSince,
                state,
                cancelledAt,
                cancellationEffectiveSince,
                cancellationInitiator,
                cancellationSource
            }.ToJson();
            _logger?.LogInformation($"{nameof(OperationExecutionPauseRepository)}, {nameof(UpdateAsync)}:\r\n" +
                json + $" Pause with oid [{oid}] has been successfully updated");
            return true;
        }

        public async Task<IEnumerable<Pause>> FindAsync(string operationId, string operationName, Func<Pause, bool> filter = null)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    var entities = await conn.QueryAsync<PauseEntity>(@$"
select Oid, OperationId, OperationName, Source, CancellationSource, CreatedAt, EffectiveSince, State, Initiator, CancelledAt, CancellationEffectiveSince, CancellationInitiator from [dbo].[MarginTradingExecutionPause]
where OperationId = @OperationId AND OperationName = @OperationName", new {OperationId = operationId, OperationName = operationName});

                    var result = entities.Select(Convert);
                    
                    if (filter != null)
                        result = result.Where(filter);

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(FindAsync)}\r\n " +
                                  $"Exception Message: {ex.Message}\r\n" +
                                  $"Stack Trace: {ex.StackTrace}\r\n" +
                                  $"Timestamp UTC: {DateTime.UtcNow}");
                if (ex.InnerException != null)
                {
                    _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(FindAsync)}\r\n " +
                                      $"Exception Message: {ex.InnerException.Message}\r\n" +
                                      $"Stack Trace: {ex.InnerException.StackTrace}\r\n" +
                                      $"Timestamp UTC: {DateTime.UtcNow}");
                }
                throw;
            }
        }

        public async Task<Pause> FindAsync(long oid)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    var entity = await conn.QuerySingleAsync<PauseEntity>($@"
select Oid, OperationId, OperationName, Source, CancellationSource, CreatedAt, EffectiveSince, State, Initiator, CancelledAt, CancellationEffectiveSince, CancellationInitiator from [dbo].[MarginTradingExecutionPause]
where Oid = @oid", new { oid });

                    return Convert(entity);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(FindAsync)}\r\n " +
                                  $"Exception Message: {ex.Message}\r\n" +
                                  $"Stack Trace: {ex.StackTrace}\r\n" +
                                  $"Timestamp UTC: {DateTime.UtcNow}");
                if (ex.InnerException != null)
                {
                    _logger?.LogError($"{nameof(OperationExecutionPauseRepository)}, {nameof(FindAsync)}\r\n " +
                                      $"Exception Message: {ex.InnerException.Message}\r\n" +
                                      $"Stack Trace: {ex.InnerException.StackTrace}\r\n" +
                                      $"Timestamp UTC: {DateTime.UtcNow}");
                }
                throw;
            }
        }

        private static Pause Convert(PauseEntity entity)
        {
            if (entity == null)
                return null;

            return Pause.Initialize(entity.Oid,
                entity.OperationId,
                entity.OperationName,
                entity.CreatedAt,
                entity.EffectiveSince,
                Enum.Parse<PauseState>(entity.State),
                Enum.Parse<PauseSource>(entity.Source),
                entity.Initiator,
                entity.CancelledAt,
                entity.CancellationEffectiveSince,
                string.IsNullOrEmpty(entity.CancellationInitiator) ? null : (Initiator)entity.CancellationInitiator,
                string.IsNullOrEmpty(entity.CancellationSource) ? (PauseCancellationSource?) null : Enum.Parse<PauseCancellationSource>(entity.CancellationSource));
        }
    }
}