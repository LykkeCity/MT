using System;
using Common.Log;
using Lykke.Common;
using MarginTrading.Core;

namespace MarginTrading.Services
{
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

        public void AddLog(string name, string clientId, string accountId, string input, string data)
        {
            _threadSwitcher.SwitchThread(async () =>
            {
                try
                {
                    var log = new OperationLog
                    {
                        Name = name,
                        AccountId = accountId,
                        ClientId = clientId,
                        Data = data,
                        Input = input
                    };

                    await _operationsLogRepository.AddLogAsync(log);
                }
                catch (Exception ex)
                {
                    _log.WriteErrorAsync(nameof(MarginTradingOperationsLogService), nameof(AddLog), $"{name}, clientId = {clientId}, accountId = {accountId}", ex).Wait();
                }
            });
        }
    }
}
