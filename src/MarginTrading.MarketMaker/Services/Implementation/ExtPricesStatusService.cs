using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.MarketMaker.Models.Api;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class ExtPricesStatusService : IExtPricesStatusService
    {
        private readonly IPrimaryExchangeService _primaryExchangeService;
        private readonly IBestPricesService _bestPricesService;

        public ExtPricesStatusService(IPrimaryExchangeService primaryExchangeService,
            IBestPricesService bestPricesService)
        {
            _primaryExchangeService = primaryExchangeService;
            _bestPricesService = bestPricesService;
        }

        public IReadOnlyDictionary<string, IReadOnlyList<ExtPriceStatusModel>> GetAll()
        {
            var qualities = _primaryExchangeService.GetQualities();
            var result = qualities.ToDictionary(p => p.Key,
                p => (IReadOnlyList<ExtPriceStatusModel>) p.Value.Values
                    .Select(q => new ExtPriceStatusModel
                    {
                        Exchange = q.Exchange,
                        Error = q.Error,
                        OrderbookReceived = q.OrderbookReceived,
                        HedgingPreference = q.HedgingPreference
                    }).ToList());

            var bestPrices = _bestPricesService.GetLastCalculated();
            foreach (var asset in result)
            {
                foreach (var exchange in asset.Value)
                {
                    if (bestPrices.TryGetValue((asset.Key, exchange.Exchange), out var bestPrice))
                    {
                        exchange.BestPrices =
                            new BestPricesModel {BestBid = bestPrice.BestBid, BestAsk = bestPrice.BestAsk};
                    }
                }
            }

            return result;
        }

        public IReadOnlyList<ExtPriceStatusModel> Get(string assetPairId)
        {
            return GetAll().GetValueOrDefault(assetPairId, ImmutableArray<ExtPriceStatusModel>.Empty);
        }
    }
}