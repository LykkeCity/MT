using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using MarginTrading.Core.Messages;

namespace MarginTrading.Services
{
    public class InstrumentsCache : IInstrumentsCache
    {
        private Dictionary<string, IMarginTradingAsset> _instruments = new Dictionary<string, IMarginTradingAsset>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
         
        ~InstrumentsCache()
        {
            _lockSlim?.Dispose();
        }

        public IMarginTradingAsset GetInstrumentById(string instrumentId)
        {
            IMarginTradingAsset result;
            if (TryGetAssetById(instrumentId, out result))
                return result;

            throw new InstrumentNotFoundException(instrumentId, string.Format(MtMessages.InstrumentNotFoundInCache, instrumentId));
        }

        public IEnumerable<IMarginTradingAsset> GetAll()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _instruments.Values.ToArray();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IMarginTradingAsset FindInstrument(string asset1, string asset2)
        {
            //TODO: optimize
            _lockSlim.EnterReadLock();
            try
            {
                foreach (var instrument in _instruments.Values)
                {
                    if (instrument.BaseAssetId == asset1 && instrument.QuoteAssetId == asset2)
                    {
                        return instrument;
                    }

                    if (instrument.BaseAssetId == asset2 && instrument.QuoteAssetId == asset1)
                    {
                        return instrument;
                    }
                }

                throw new InstrumentByAssetsNotFoundException(asset1, asset2, string.Format(MtMessages.InstrumentWithAssetsNotFound, asset1, asset2));
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        internal bool TryGetAssetById(string instrumentId, out IMarginTradingAsset result)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_instruments.ContainsKey(instrumentId))
                {
                    result = null;
                    return false;
                }
                result = _instruments[instrumentId];
                return true;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        internal void InitInstrumentsCache(Dictionary<string, IMarginTradingAsset> instruments)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _instruments = instruments;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}