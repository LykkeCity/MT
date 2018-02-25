using System;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    internal class MtDataReaderClientsPair : IMtDataReaderClientsPair
    {
        public IMtDataReaderClient Demo { get; }
        public IMtDataReaderClient Live { get; }

        public MtDataReaderClientsPair(IMtDataReaderClient demo, IMtDataReaderClient live)
        {
            Demo = demo ?? throw new ArgumentNullException(nameof(demo));
            Live = live ?? throw new ArgumentNullException(nameof(live));
        }
    }
}