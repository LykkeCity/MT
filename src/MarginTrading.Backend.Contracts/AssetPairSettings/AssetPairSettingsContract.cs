using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.AssetPairSettings
{
    [PublicAPI]
    public class AssetPairSettingsContract : AssetPairSettingsInputContract
    {
        /// <summary>
        /// Instrument id
        /// </summary>
        public string AssetPairId { get; set; }
    }
}