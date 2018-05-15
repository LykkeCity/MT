using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common;

namespace MarginTrading.Common.Services
{
    [UsedImplicitly]
    public sealed class MarginTradingOperationsLogService : IMarginTradingOperationsLogService
    {
        private readonly IMarginTradingOperationsLogRepository _operationsLogRepository;
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly ILog _log;

        public MarginTradingOperationsLogService(
            IMarginTradingOperationsLogRepository operationsLogRepository,
            IThreadSwitcher threadSwitcher,
            ILog log)
        {
            _operationsLogRepository = operationsLogRepository;
            _threadSwitcher = threadSwitcher;
            _log = log;
        }

        public void AddLog(string name, string accountId, string input, string data)
        {
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
                    _log.WriteErrorAsync(nameof(MarginTradingOperationsLogService), nameof(AddLog), $"{name}, accountId = {accountId}", ex).Wait();
                }
            });
        }
    }
}
