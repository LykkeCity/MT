using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core
{
    public class AssetPairSettings
    {
        public string LegalEntity { get; }
        
        public MatchingEngineMode MatchingEngineMode { get; }
        
        public AssetPairSettings(string legalEntity, MatchingEngineMode matchingEngineMode)
        {
            LegalEntity = legalEntity;
            MatchingEngineMode = matchingEngineMode;
        }
    }
}