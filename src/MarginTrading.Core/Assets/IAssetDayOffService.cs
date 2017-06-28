using System;

namespace MarginTrading.Core.Assets
{
    public interface IAssetDayOffService
    {
        bool IsDayOff(string assetId);
    }
}
