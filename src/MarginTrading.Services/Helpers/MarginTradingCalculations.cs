using System;

namespace MarginTrading.Services.Helpers
{
    public static class MarginTradingCalculations
    {
        public static double GetVolumeFromPoints(double amount, int accuracy)
        {
            var val = Math.Pow(10, -accuracy);
            return val * amount;
        }
    }
}
