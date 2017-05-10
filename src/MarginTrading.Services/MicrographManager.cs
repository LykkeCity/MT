using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class MicrographManager : TimerPeriod
    {
        private readonly MicrographCacheService _micrographCacheService;
        private readonly IMarginTradingBlobRepository _blobRepository;

        public MicrographManager(
            MicrographCacheService micrographCacheService,
            IMarginTradingBlobRepository blobRepository,
            ILog log) : base(nameof(MicrographManager), 60000, log)
        {
            _micrographCacheService = micrographCacheService;
            _blobRepository = blobRepository;
        }

        public override void Start()
        {
            var graphData = _blobRepository.Read<Dictionary<string, List<GraphBidAskPair>>>("prices", "graph") ??
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
