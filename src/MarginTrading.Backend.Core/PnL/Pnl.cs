// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.PnL
{
    public abstract class PnlBase
    {
        public decimal EntryPrice { get; }
        public decimal CurrentPrice { get; }
        public decimal Volume { get; }
        public decimal FxRate { get; }
        public decimal Value { get; protected set; }

        protected virtual decimal Calculate()
        {
            return (CurrentPrice - EntryPrice) * Volume * FxRate;
        }

        protected PnlBase(decimal entryPrice, decimal currentPrice, decimal volume, decimal fxRate)
        {
            if (entryPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(entryPrice), "Entry price must be positive");
            
            if (currentPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(currentPrice), "Current price must be positive");
            
            if (volume <= 0)
                throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be positive");
            
            if (fxRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(fxRate), "Fx rate must be positive");

            EntryPrice = entryPrice;
            CurrentPrice = currentPrice;
            Volume = volume;
            FxRate = fxRate;
        }

        public static implicit operator decimal(PnlBase source) => source.Value;
    }
}