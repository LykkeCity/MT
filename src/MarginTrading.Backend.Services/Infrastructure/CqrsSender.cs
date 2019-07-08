// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class CqrsSender : ICqrsSender
    {
        [NotNull] public ICqrsEngine CqrsEngine { get; set; }//property injection
        [NotNull] private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;

        public CqrsSender([NotNull] CqrsContextNamesSettings cqrsContextNamesSettings)
        {
            _cqrsContextNamesSettings = cqrsContextNamesSettings ??
                throw new ArgumentNullException(nameof(cqrsContextNamesSettings));
        }

        public void SendCommandToAccountManagement<T>(T command)
        {
            CqrsEngine.SendCommand(command, _cqrsContextNamesSettings.TradingEngine,
                _cqrsContextNamesSettings.AccountsManagement);
        }

        public void SendCommandToSettingsService<T>(T command)
        {
            CqrsEngine.SendCommand(command, _cqrsContextNamesSettings.TradingEngine,
                _cqrsContextNamesSettings.SettingsService);
        }
        
        public void SendCommandToSelf<T>(T command)
        {
            CqrsEngine.SendCommand(command, _cqrsContextNamesSettings.TradingEngine,
                _cqrsContextNamesSettings.TradingEngine);
        }

        public void PublishEvent<T>(T ev, string boundedContext = null)
        {
            try
            {
                CqrsEngine.PublishEvent(ev, boundedContext ?? _cqrsContextNamesSettings.TradingEngine);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}