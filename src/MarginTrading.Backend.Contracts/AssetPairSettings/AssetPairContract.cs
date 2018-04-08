using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.AssetPairSettings
{
    [PublicAPI]
    public class AssetPairContract : AssetPairInputContract
    {
        /// <summary>
        /// Instrument id
        /// </summary>
        public string Id { get; set; }
    }
}