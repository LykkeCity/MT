using System.Collections.Generic;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core.Orders
{
    public class PositionsCloseData
    {
        public PositionsCloseData(
            List<Position> positions,
            string accountId, 
            string assetPairId, 
            decimal volume,
            string openMatchingEngineId, 
            string externalProviderId,
            OriginatorType originator, 
            string additionalInfo, 
            string correlationId, string equivalentAsset, string comment = null, 
            IMatchingEngineBase matchingEngine = null, 
            OrderModality modality = OrderModality.Regular)
        {
            Positions = positions;
            AccountId = accountId;
            AssetPairId = assetPairId;
            Volume = volume;
            OpenMatchingEngineId = openMatchingEngineId;
            ExternalProviderId = externalProviderId;
            Originator = originator;
            AdditionalInfo = additionalInfo;
            CorrelationId = correlationId;
            EquivalentAsset = equivalentAsset;
            Comment = comment;
            MatchingEngine = matchingEngine;
            Modality = modality;
        }

        public List<Position> Positions { get; }
        
        public string AccountId { get; }
        
        public string AssetPairId { get; }
        
        public decimal Volume { get; }

        public string OpenMatchingEngineId { get; }
        
        public string ExternalProviderId { get; }
        
        public string EquivalentAsset { get; }
        
        public OriginatorType Originator { get; }
        
        public string AdditionalInfo { get; }
        
        public string CorrelationId { get; }
        
        public string Comment { get; }
        
        public IMatchingEngineBase MatchingEngine { get; }
        
        public OrderModality Modality { get; }
    }
}