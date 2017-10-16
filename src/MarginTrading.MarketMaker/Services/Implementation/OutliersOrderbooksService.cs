using System.Collections.Generic;
using System.Collections.Immutable;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class OutliersOrderbooksService : IOutliersOrderbooksService
    {
        public IReadOnlyList<ExternalOrderbook> FindOutliers(ImmutableDictionary<string, ExternalOrderbook> validOrderbooks)
        {
            throw new System.NotImplementedException();
        }
    }
}