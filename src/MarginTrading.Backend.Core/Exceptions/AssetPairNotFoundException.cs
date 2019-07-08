// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class AssetPairNotFoundException : Exception
    {
        public string InstrumentId { get; private set; }

        public AssetPairNotFoundException(string instrumentId, string message):base(message)
        {
            InstrumentId = instrumentId;
        }
    }
}