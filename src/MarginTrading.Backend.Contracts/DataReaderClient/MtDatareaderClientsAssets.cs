using System;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    internal class MtDataReaderClientsAssets : IMtDataReaderClientsAssets
    {
        public IMtDataReaderClient Demo { get; }
        public IMtDataReaderClient Live { get; }

        public IMtDataReaderClient Get(bool isLive)
        {
            return isLive ? Live : Demo;
        }

        public MtDataReaderClientsAssets(IMtDataReaderClient demo, IMtDataReaderClient live)
        {
            Demo = demo ?? throw new ArgumentNullException(nameof(demo));
            Live = live ?? throw new ArgumentNullException(nameof(live));
        }
    }
}