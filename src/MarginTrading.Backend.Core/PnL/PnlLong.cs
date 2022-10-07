// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.PnL
{
    public sealed class PnlLong : PnlBase
    {
        public PnlLong(decimal entryPrice, decimal currentPrice, decimal volume, decimal fxRate) 
            : base(entryPrice, currentPrice, volume, fxRate)
        {
            Value = Calculate();
        }
    }
}