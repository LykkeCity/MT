// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.AssetService.Contracts.Products;
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
        /// Market identifier.
        /// </summary>
        string MarketId { get; }

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

        string AssetType { get; }

    }

    public class AssetPair : IAssetPair
    {
        public AssetPair(string id, string name, string baseAssetId, string quoteAssetId, int accuracy, string marketId,
            string legalEntity, [CanBeNull] string basePairId, MatchingEngineMode matchingEngineMode,
            decimal stpMultiplierMarkupBid, decimal stpMultiplierMarkupAsk, bool isSuspended, bool isFrozen,
            bool isDiscontinued, string assetType)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BaseAssetId = baseAssetId ?? throw new ArgumentNullException(nameof(baseAssetId));
            QuoteAssetId = quoteAssetId ?? throw new ArgumentNullException(nameof(quoteAssetId));
            Accuracy = accuracy;
            MarketId = marketId;
            LegalEntity = legalEntity.RequiredNotNullOrWhiteSpace(nameof(legalEntity));
            BasePairId = basePairId;
            MatchingEngineMode = matchingEngineMode.RequiredEnum(nameof(matchingEngineMode));
            StpMultiplierMarkupBid = stpMultiplierMarkupBid.RequiredGreaterThan(0, nameof(stpMultiplierMarkupBid));
            StpMultiplierMarkupAsk = stpMultiplierMarkupAsk.RequiredGreaterThan(0, nameof(stpMultiplierMarkupAsk));

            IsSuspended = isSuspended;
            IsFrozen = isFrozen;
            IsDiscontinued = isDiscontinued;
            AssetType = assetType;
        }

        public string Id { get; }
        public string Name { get; }
        public string BaseAssetId { get; }
        public string QuoteAssetId { get; }
        public int Accuracy { get; }
        public string MarketId { get; }

        public string LegalEntity { get; }
        public string BasePairId { get; }
        public MatchingEngineMode MatchingEngineMode { get; }
        public decimal StpMultiplierMarkupBid { get; }
        public decimal StpMultiplierMarkupAsk { get; }

        public bool IsSuspended { get; set; }
        public bool IsFrozen { get; }
        public bool IsDiscontinued { get; }
        public string AssetType { get; }

        protected bool Equals(AssetPair other)
        {
            return string.Equals(Id, other.Id) && string.Equals(Name, other.Name) &&
                   string.Equals(BaseAssetId, other.BaseAssetId) && string.Equals(QuoteAssetId, other.QuoteAssetId) &&
                   Accuracy == other.Accuracy && string.Equals(LegalEntity, other.LegalEntity) &&
                   string.Equals(BasePairId, other.BasePairId) && MatchingEngineMode == other.MatchingEngineMode &&
                   StpMultiplierMarkupBid == other.StpMultiplierMarkupBid &&
                   StpMultiplierMarkupAsk == other.StpMultiplierMarkupAsk && IsSuspended == other.IsSuspended &&
                   IsFrozen == other.IsFrozen && IsDiscontinued == other.IsDiscontinued &&
                   AssetType == other.AssetType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetPair)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static IAssetPair CreateFromProduct(ProductContract product, string legalEntity)
        {
            return new AssetPair(
                id: product.ProductId,
                name: product.Name,
                baseAssetId: product.ProductId,
                quoteAssetId: product.TradingCurrency,
                accuracy: AssetPairConstants.Accuracy,
                marketId: product.Market,
                legalEntity: legalEntity,
                basePairId: AssetPairConstants.BasePairId,
                matchingEngineMode: AssetPairConstants.MatchingEngineMode,
                stpMultiplierMarkupBid: AssetPairConstants.StpMultiplierMarkupBid,
                stpMultiplierMarkupAsk: AssetPairConstants.StpMultiplierMarkupAsk,
                isSuspended: product.IsSuspended,
                isFrozen: product.IsFrozen,
                isDiscontinued: product.IsDiscontinued,
                assetType: product.AssetType
            );
        }

        public static IAssetPair CreateFromCurrency(string currencyId, string legalEntity)
        {
            var id = $"{AssetPairConstants.BaseCurrencyId}{currencyId}";

            return new AssetPair(
                id: id,
                name: id,
                baseAssetId: AssetPairConstants.BaseCurrencyId,
                quoteAssetId: currencyId,
                accuracy: AssetPairConstants.Accuracy,
                marketId: AssetPairConstants.FxMarketId,
                legalEntity: legalEntity,
                basePairId: AssetPairConstants.BasePairId,
                matchingEngineMode: AssetPairConstants.MatchingEngineMode,
                stpMultiplierMarkupBid: AssetPairConstants.StpMultiplierMarkupBid,
                stpMultiplierMarkupAsk: AssetPairConstants.StpMultiplierMarkupAsk,
                isSuspended: false,
                isFrozen: false,
                isDiscontinued: false,
                assetType: null
                );
        }
    }
}
