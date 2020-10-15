using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AssetService.Contracts.ClientProfileSettings;
using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services.Workflow
{
    public class ClientProfileSettingsChangedProjection
    {
        private readonly ITradingInstrumentsManager _tradingInstrumentsManager;

        public ClientProfileSettingsChangedProjection(ITradingInstrumentsManager tradingInstrumentsManager)
        {
            _tradingInstrumentsManager = tradingInstrumentsManager;
        }

        [UsedImplicitly]
        public async Task Handle(ClientProfileSettingsChangedEvent e)
        {
            switch (e.ChangeType)
            {
                case ChangeType.Creation:
                    break;
                case ChangeType.Edition:
                    if (e.NewValue.Margin != e.OldValue.Margin || e.NewValue.IsAvailable != e.OldValue.IsAvailable)
                        await _tradingInstrumentsManager.UpdateTradingInstrumentsCacheAsync();
                    break;
                case ChangeType.Deletion:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}