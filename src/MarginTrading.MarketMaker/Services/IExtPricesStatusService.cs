using System.Collections.Generic;
using MarginTrading.MarketMaker.Models.Api;

namespace MarginTrading.MarketMaker.Services
{
    public interface IExtPricesStatusService
    {
        IReadOnlyDictionary<string, IReadOnlyList<ExtPriceStatusModel>> GetAll();
        IReadOnlyList<ExtPriceStatusModel> Get(string assetPairId);
    }
}