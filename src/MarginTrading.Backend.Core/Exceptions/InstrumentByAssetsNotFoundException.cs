using System;
using MarginTrading.Backend.Core.Messages;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class InstrumentByAssetsNotFoundException : Exception
    {
        public string Asset1 { get; }
        public string Asset2 { get; }
        public string LegalEntityId { get; }

        public InstrumentByAssetsNotFoundException(string asset1, string asset2, string legalEntityId) : base(
            string.Format(MtMessages.InstrumentWithAssetsNotFound, asset1, asset2, legalEntityId))
        {
            Asset1 = asset1;
            Asset2 = asset2;
            LegalEntityId = legalEntityId;
        }
    }
}