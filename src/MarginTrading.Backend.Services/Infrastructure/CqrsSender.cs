using System;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class CqrsSender : ICqrsSender
    {
        [NotNull] private readonly ICqrsEngine _cqrsEngine;
        [NotNull] private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;

        public CqrsSender([NotNull] ICqrsEngine cqrsEngine, [NotNull] CqrsContextNamesSettings cqrsContextNamesSettings)
        {
            _cqrsEngine = cqrsEngine ?? throw new ArgumentNullException(nameof(cqrsEngine));
            _cqrsContextNamesSettings = cqrsContextNamesSettings ??
                throw new ArgumentNullException(nameof(cqrsContextNamesSettings));
        }

        public void SendCommandToAccountManagement<T>(T command)
        {
            _cqrsEngine.SendCommand(command, _cqrsContextNamesSettings.TradingEngine,
                _cqrsContextNamesSettings.AccountsManagement);
        }

        public void SendCommandToSettingsService<T>(T command)
        {
            _cqrsEngine.SendCommand(command, _cqrsContextNamesSettings.TradingEngine,
                _cqrsContextNamesSettings.SettingsService);
        }

        public void PublishEvent<T>(T ev)
        {
            try
            {
                _cqrsEngine.PublishEvent(ev, _cqrsContextNamesSettings.TradingEngine);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}