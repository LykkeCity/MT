using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Client
{
    [PublicAPI]
    public interface IMtBackendClient
    {
        IScheduleSettingsApi ScheduleSettings { get; }
        IAssetPairSettingsEditingApi AssetPairSettingsEdit { get; }
    }
}