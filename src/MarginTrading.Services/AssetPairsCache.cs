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
            _lockSlim.EnterReadLock();

            try
            {
                if (_assetPairs.TryGetValue(assetPairId, out var result))
                    return result;

                throw new AssetPairNotFoundException(assetPairId,
                    string.Format(MtMessages.InstrumentNotFoundInCache, assetPairId));
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
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

        public IAssetPair FindAssetPair(string asset1, string asset2)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (_assetPairs.TryGetValue(GetAssetPairId(asset1, asset2), out var result))
                    return result;

                if (_assetPairs.TryGetValue(GetAssetPairId(asset2, asset1), out result))
                    return result;

                throw new InstrumentByAssetsNotFoundException(asset1, asset2, string.Format(MtMessages.InstrumentWithAssetsNotFound, asset1, asset2));
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

        private string GetAssetPairId(string asset1, string asset2)
        {
            return $"{asset1}{asset2}";
        }
    }
}