// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.PnL
{
    public sealed class PnlShort : PnlBase
    {
        public PnlShort(decimal entryPrice, decimal currentPrice, decimal volume, decimal fxRate) 
            : base(entryPrice, currentPrice, volume, fxRate)
        {
            Value = Calculate();
        }
        
        protected override decimal Calculate()
        {
            return base.Calculate() * -1;
        }
    }
}