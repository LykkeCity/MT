// Copyright (c) 2019 Lykke Corp.

using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Common.Services;

namespace MarginTrading.SqlRepositories.Repositories
{
    /// <summary>
    /// Write log to the current Serilog output.
    /// Serilog logger must be registered as ILog.
    /// </summary>
    public class SerilogOperationsLogRepository : IOperationsLogRepository
    {
        private readonly ILog _log;
        
        public SerilogOperationsLogRepository(ILog log)
        {
            _log = log;
        }
        
        public async Task AddLogAsync(IOperationLog logEntity)
        {
            await _log.WriteInfoAsync(nameof(SerilogOperationsLogRepository), logEntity.Name,
                logEntity.Input, logEntity.Data);
        }
    }
}