using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using MarginTrading.Core.Messages;

namespace MarginTrading.Services
{
    public class AssetPairsCache : IAssetPairsCache
    {
        private Dictionary<string, IMarginTradingAssetPair> _assetPairs = new Dictionary<string, IMarginTradingAssetPair>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        ~AssetPairsCache()
        {
            _lockSlim?.Dispose();
        }

        public IMarginTradingAssetPair GetAssetPairById(string assetPairId)
        {
            if (TryGetAssetById(assetPairId, out var result))
            {
                return result;
            }

            throw new AssetPairNotFoundException(assetPairId, string.Format(MtMessages.InstrumentNotFoundInCache, assetPairId));
        }

        public IEnumerable<IMarginTradingAssetPair> GetAll()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _assetPairs.Values.ToArray();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IMarginTradingAssetPair FindInstrument(string asset1, string asset2)
        {
            //TODO: optimize
            _lockSlim.EnterReadLock();
            try
            {
                foreach (var instrument in _assetPairs.Values)
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

        internal bool TryGetAssetById(string instrumentId, out IMarginTradingAssetPair result)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_assetPairs.ContainsKey(instrumentId))
                {
                    result = null;
                    return false;
                }
                result = _assetPairs[instrumentId];
                return true;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        internal void InitInstrumentsCache(Dictionary<string, IMarginTradingAssetPair> instruments)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _assetPairs = instruments;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}