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
        private Dictionary<string, IAssetPair> _assetPairs = new Dictionary<string, IAssetPair>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public IAssetPair GetAssetPairById(string assetPairId)
        {
            if (TryGetAssetById(assetPairId, out var result))
            {
                return result;
            }

            throw new AssetPairNotFoundException(assetPairId, string.Format(MtMessages.InstrumentNotFoundInCache, assetPairId));
        }

        public IEnumerable<IAssetPair> GetAll()
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

        public IAssetPair FindInstrument(string asset1, string asset2)
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

        internal bool TryGetAssetById(string instrumentId, out IAssetPair result)
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

        internal void InitInstrumentsCache(Dictionary<string, IAssetPair> instruments)
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