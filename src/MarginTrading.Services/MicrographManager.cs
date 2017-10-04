using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core;
using System.Linq;

namespace MarginTrading.Services
{
    public class MicrographManager : TimerPeriod
    {
        private readonly MicrographCacheService _micrographCacheService;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly IAccountAssetsCacheService _accountAssetsCache;

        public MicrographManager(
            MicrographCacheService micrographCacheService,
            IMarginTradingBlobRepository blobRepository,
            ILog log,
            IAccountAssetsCacheService accountAssetsCache) : base(nameof(MicrographManager), 60000, log)
        {
            _micrographCacheService = micrographCacheService;
            _blobRepository = blobRepository;
            _accountAssetsCache = accountAssetsCache;
        }

        public override void Start()
        {
            var graphData = _blobRepository.Read<Dictionary<string, List<GraphBidAskPair>>>("prices", "graph")
                                ?.ToDictionary(d => d.Key, d => d.Value) ??
                            new Dictionary<string, List<GraphBidAskPair>>();

            if (graphData.Count > 0)
            {
                FixGraphData(graphData);
                _micrographCacheService.InitCache(graphData);
            }

            base.Start();
        }

        public override async Task Execute()
        {
            var dataToWrite = _micrographCacheService.GetGraphData();
            await _blobRepository.Write("prices", "graph", dataToWrite);
        }

        private void FixGraphData(Dictionary<string, List<GraphBidAskPair>> graphData)
        {
            foreach (var pair in graphData)
            {
                for (int i = pair.Value.Count - 1; i >= 0; i--)
                {
                    var bidAsk = pair.Value[i];

                    if (bidAsk.Bid > bidAsk.Ask)
                    {
                        graphData[pair.Key].Remove(bidAsk);
                    }
                }
            }
        }
    }
}
