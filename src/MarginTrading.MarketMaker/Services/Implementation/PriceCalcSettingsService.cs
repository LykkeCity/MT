using System;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    class PriceCalcSettingsService : IPriceCalcSettingsService
    {
        public bool IsStepEnabled(OrderbookGeneratorStepEnum step, string assetPairId)
        {
            // todo check if is fiat
            // todo check settings
            throw new System.NotImplementedException();
        }

        public string GetPresetPrimaryExchange(string assetPairId)
        {
            throw new System.NotImplementedException();
        }

        public decimal GetVolumeMultiplier(string assetPairId, string exchangeName)
        {
            throw new System.NotImplementedException();
        }

        public TimeSpan GetOrderbookAgeThreshold(string assetPairId, string exchangeName, DateTime now)
        {
            throw new NotImplementedException();
        }

        public RepeatedOutliersParams GetRepeatedOutliersParams(string assetPairId)
        {
            throw new NotImplementedException();
        }

        public decimal GetOutlierThreshold(string assetPairId)
        {
            throw new NotImplementedException();
        }
    }
}