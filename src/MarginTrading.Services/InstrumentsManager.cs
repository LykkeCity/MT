using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class InstrumentsManager : IStartable
    {
        private readonly IMarginTradingAssetsRepository _repository;
        private readonly InstrumentsCache _instrumentsCache;

        public InstrumentsManager(IMarginTradingAssetsRepository repository,
            InstrumentsCache instrumentsCache)
        {
            _repository = repository;
            _instrumentsCache = instrumentsCache;
        }

        public void Start()
        {
            UpdateInstrumentsCache().Wait();
        }

        public async Task UpdateInstrumentsCache()
        {
            var instruments = (await _repository.GetAllAsync()).ToDictionary(x => x.Id);
            _instrumentsCache.InitInstrumentsCache(instruments);
        }
    }
}
