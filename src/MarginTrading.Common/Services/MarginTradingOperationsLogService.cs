using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common;

namespace MarginTrading.Common.Services
{
    [UsedImplicitly]
    public sealed class OperationsLogService : IOperationsLogService
    {
        private readonly IOperationsLogRepository _operationsLogRepository;
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly ILog _log;
        private readonly bool _writeOperationLog;

        public OperationsLogService(
            IOperationsLogRepository operationsLogRepository,
            IThreadSwitcher threadSwitcher,
            ILog log,
            bool writeOperationLog)
        {
            _operationsLogRepository = operationsLogRepository;
            _threadSwitcher = threadSwitcher;
            _log = log;
            _writeOperationLog = writeOperationLog;
        }

        public void AddLog(string name, string accountId, string input, string data)
        {
            if (!_writeOperationLog)
            {
                return;
            }
            
            _threadSwitcher.SwitchThread(async () =>
            {
                try
                {
                    var log = new OperationLog
                    {
                        Name = name,
                        AccountId = accountId,
                        Data = data,
                        Input = input
                    };

                    await _operationsLogRepository.AddLogAsync(log);
                }
                catch (Exception ex)
                {
                    _log.WriteErrorAsync(nameof(OperationsLogService), nameof(AddLog), $"{name}, accountId = {accountId}", ex).Wait();
                }
            });
        }
    }
}
