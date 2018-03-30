using System;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Helpers
{
    public static class OvernightSwapHelpers
    {

        public static (int Hour, int Min) GetOvernightSwapCalcTime(string settings)
        {
            var splittedString = settings?.Split(':') ?? new string[0];
            if (splittedString.Length == 2
                && int.TryParse(splittedString[0], out var h) && int.TryParse(splittedString[1], out var m))
                return (h, m);
			
            LogLocator.CommonLog?.WriteErrorAsync(nameof(OvernightSwapHelpers), nameof(GetOvernightSwapCalcTime),
                new Exception("Can not parse OvernightSwapCalcTime from settings."), DateTime.UtcNow);
            return (0, 0);
        }
    }
}