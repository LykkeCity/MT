// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Messages;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class InstrumentByAssetsNotFoundException : Exception
    {
        public string Asset1 { get; }
        public string Asset2 { get; }
        public string LegalEntity { get; }

        public InstrumentByAssetsNotFoundException(string asset1, string asset2, string legalEntity)
            :base(string.Format(MtMessages.InstrumentWithAssetsNotFound, asset1, asset2, legalEntity))
        {
            Asset1 = asset1;
            Asset2 = asset2;
            LegalEntity = legalEntity;
        }
    }
}