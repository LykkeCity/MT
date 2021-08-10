using JetBrains.Annotations;
using MarginTrading.AssetService.Contracts.ClientProfileSettings;
using MarginTrading.AssetService.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.Workflow
{
    public class ClientProfileSettingsProjection
    {
        private readonly IClientProfileSettingsCache _clientProfileSettingsCache;

        public ClientProfileSettingsProjection(IClientProfileSettingsCache clientProfileSettingsCache)
        {
            _clientProfileSettingsCache = clientProfileSettingsCache;
        }

        [UsedImplicitly]
        public Task Handle(ClientProfileSettingsChangedEvent @event)
        {
            switch (@event.ChangeType)
            {
                case ChangeType.Creation:
                    _clientProfileSettingsCache.AddOrUpdate(@event.NewValue);
                    break;
                case ChangeType.Edition:
                    _clientProfileSettingsCache.AddOrUpdate(@event.NewValue);
                    break;
                case ChangeType.Deletion:
                    _clientProfileSettingsCache.Remove(@event.OldValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.CompletedTask;
        }
    }
}
