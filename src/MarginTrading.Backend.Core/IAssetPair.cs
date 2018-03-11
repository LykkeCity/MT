using System;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPair
    {
        string Id { get; }
        string Name { get; }
        string BaseAssetId { get; }
        string QuoteAssetId { get; }
        int Accuracy { get; }
    }

    public class AssetPair : IAssetPair
    {
        public AssetPair([NotNull] string id, [NotNull] string name, [NotNull] string baseAssetId,
            [NotNull] string quoteAssetId, int accuracy)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BaseAssetId = baseAssetId ?? throw new ArgumentNullException(nameof(baseAssetId));
            QuoteAssetId = quoteAssetId ?? throw new ArgumentNullException(nameof(quoteAssetId));
            Accuracy = accuracy;
        }

        [NotNull] public string Id { get; }
        [NotNull] public string Name { get; }
        [NotNull] public string BaseAssetId { get; }
        [NotNull] public string QuoteAssetId { get; }
        public int Accuracy { get; }
    }
}
