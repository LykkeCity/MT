using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using MarginTrading.AzureRepositories.Contract;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public class TradingConditionsManager : IStartable
    {
        private readonly ITradingConditionRepository _repository;
        private readonly TradingConditionsCacheService _tradingConditionsCacheService;
        private readonly IConsole _console;

        public TradingConditionsManager(
            ITradingConditionRepository repository,
            TradingConditionsCacheService tradingConditionsCacheService,
            IConsole console)
        {
            _repository = repository;
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _console = console;
        }

        public void Start()
        {
            InitTradingConditions().Wait();
        }

        private async Task InitTradingConditions()
        {
            _console.WriteLine($"Started {nameof(InitTradingConditions)}");
            
            var tradingConditions = (await _repository.GetAllAsync()).ToList();
            _tradingConditionsCacheService.InitTradingConditionsCache(tradingConditions);
            
            _console.WriteLine(
                $"Finished {nameof(InitTradingConditions)}. Count:{tradingConditions.Count})");
        }
    }
}
