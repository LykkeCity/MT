using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AssetService.Contracts.ClientProfiles;
using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services.Workflow
{
    public class ClientProfileChangedProjection
    {
        private readonly ITradingInstrumentsManager _tradingInstrumentsManager;

        public ClientProfileChangedProjection(ITradingInstrumentsManager tradingInstrumentsManager)
        {
            _tradingInstrumentsManager = tradingInstrumentsManager;
        }

        [UsedImplicitly]
        public async Task Handle(ClientProfileChangedEvent e)
        {
            switch (e.ChangeType)
            {
                case ChangeType.Creation:
                case ChangeType.Edition:
                    if (e.NewValue.IsDefault && (e.OldValue == null || !e.OldValue.IsDefault))
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