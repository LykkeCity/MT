using System;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    internal class MtDataReaderClientsPair : IMtDataReaderClientsPair
    {
        public IMtDataReaderClient Demo { get; }
        public IMtDataReaderClient Live { get; }

        public IMtDataReaderClient Get(bool isLive)
        {
            return isLive ? Live : Demo;
        }

        public MtDataReaderClientsPair(IMtDataReaderClient demo, IMtDataReaderClient live)
        {
            Demo = demo ?? throw new ArgumentNullException(nameof(demo));
            Live = live ?? throw new ArgumentNullException(nameof(live));
        }
    }
}