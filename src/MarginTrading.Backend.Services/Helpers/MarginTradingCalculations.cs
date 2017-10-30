using System;

namespace MarginTrading.Backend.Services.Helpers
{
    public static class MarginTradingCalculations
    {
        public static decimal GetVolumeFromPoints(decimal amount, int accuracy)
        {
            var val = Math.Pow(10, -accuracy);
            return (decimal) val * amount;
        }
    }
}
