using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPair
    {
        /// <summary>
        /// Instrument id
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Instrument display name
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Base asset id
        /// </summary>
        string BaseAssetId { get; }
        
        /// <summary>
        /// Quoting asset id
        /// </summary>
        string QuoteAssetId { get; }
        
        /// <summary>
        /// Instrument accuracy in decimal digits count
        /// </summary>
        int Accuracy { get; }

        /// <summary>
        /// Id of legal entity
        /// </summary>
        string LegalEntity { get; }

        /// <summary>
        /// Base pair id (ex. BTCUSD for id BTCUSD.cy)
        /// </summary>
        [CanBeNull]
        string BasePairId { get; }

        /// <summary>
        /// How should this asset pair be traded
        /// </summary>
        MatchingEngineMode MatchingEngineMode { get; }

        /// <summary>
        /// Markup for bid for stp mode. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        decimal StpMultiplierMarkupBid { get; }

        /// <summary>
        /// Markup for ask for stp mode. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        decimal StpMultiplierMarkupAsk { get; }
        
        /// <summary>
        /// Asset pair is blocked due to a zero quote
        /// </summary>
        /// <remarks>The property is mutable</remarks>
        bool IsSuspended { get; set; }
        
        /// <summary>
        /// Asset pair is blocked by API call for some time
        /// </summary>
        bool IsFrozen { get; }
        
        /// <summary>
        /// Asset pair is blocked by API call, for all time in most cases
        /// </summary>
        bool IsDiscontinued { get; }
        
        /// <summary>
        /// Current asset pair state depending on <see cref="IsSuspended"/>, <see cref="IsFrozen"/> and <see cref="IsDiscontinued"/>
        /// </summary>
        bool IsDisabled { get; }
    }

    public class AssetPair : IAssetPair
    {
        public AssetPair(string id, string name, string baseAssetId,
            string quoteAssetId, int accuracy, string legalEntity,
            [CanBeNull] string basePairId, MatchingEngineMode matchingEngineMode, decimal stpMultiplierMarkupBid,
            decimal stpMultiplierMarkupAsk, bool isSuspended, bool isFrozen, bool isDiscontinued)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BaseAssetId = baseAssetId ?? throw new ArgumentNullException(nameof(baseAssetId));
            QuoteAssetId = quoteAssetId ?? throw new ArgumentNullException(nameof(quoteAssetId));
            Accuracy = accuracy;
            
            LegalEntity = legalEntity.RequiredNotNullOrWhiteSpace(nameof(legalEntity));
            BasePairId = basePairId;
            MatchingEngineMode = matchingEngineMode.RequiredEnum(nameof(matchingEngineMode));
            StpMultiplierMarkupBid = stpMultiplierMarkupBid.RequiredGreaterThan(0, nameof(stpMultiplierMarkupBid));
            StpMultiplierMarkupAsk = stpMultiplierMarkupAsk.RequiredGreaterThan(0, nameof(stpMultiplierMarkupAsk));
            
            IsSuspended = isSuspended;
            IsFrozen = isFrozen;
            IsDiscontinued = isDiscontinued;
        }

        public string Id { get; }
        public string Name { get; }
        public string BaseAssetId { get; }
        public string QuoteAssetId { get; }
        public int Accuracy { get; }
        
        public string LegalEntity { get; }
        public string BasePairId { get; }
        public MatchingEngineMode MatchingEngineMode { get; }
        public decimal StpMultiplierMarkupBid { get; }
        public decimal StpMultiplierMarkupAsk { get; }
        
        public bool IsSuspended { get; set; }
        public bool IsFrozen { get; }
        public bool IsDiscontinued { get; }
        
        public bool IsDisabled => IsSuspended || IsFrozen || IsDiscontinued; 
        
        protected bool Equals(AssetPair other)
        {
            return string.Equals(Id, other.Id) && string.Equals(Name, other.Name) &&
                   string.Equals(BaseAssetId, other.BaseAssetId) && string.Equals(QuoteAssetId, other.QuoteAssetId) &&
                   Accuracy == other.Accuracy && string.Equals(LegalEntity, other.LegalEntity) &&
                   string.Equals(BasePairId, other.BasePairId) && MatchingEngineMode == other.MatchingEngineMode &&
                   StpMultiplierMarkupBid == other.StpMultiplierMarkupBid &&
                   StpMultiplierMarkupAsk == other.StpMultiplierMarkupAsk && IsSuspended == other.IsSuspended &&
                   IsFrozen == other.IsFrozen && IsDiscontinued == other.IsDiscontinued;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetPair) obj);
        }
    }
}
