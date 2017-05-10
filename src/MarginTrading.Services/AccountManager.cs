using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class AccountManager: IStartable
    {
        private readonly AccountsCacheService _cache;
        private readonly IMarginTradingAccountsRepository _repository;
        private readonly IConsole _console;
        private readonly MarginSettings _marginSettings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;

        public AccountManager(AccountsCacheService cacheService,
            IMarginTradingAccountsRepository repository,
            IConsole console,
            MarginSettings marginSettings,
            IRabbitMqNotifyService rabbitMqNotifyService)
        {
            _cache = cacheService;
            _repository = repository;
            _console = console;
            _marginSettings = marginSettings;
            _rabbitMqNotifyService = rabbitMqNotifyService;
        }

        public void Start()
        {
            var accounts = _repository.GetAllAsync().Result.Select(MarginTradingAccount.Create).GroupBy(x => x.ClientId).ToDictionary(x => x.Key, x => x.ToArray());

            _cache.InitAccountsCache(accounts);

            _console.WriteLine($"InitAccountsCache (clients count:{accounts.Count})");
        }

        public async Task UpdateAccountsCacheAsync(string clientId)
        {
            var accounts = (await _repository.GetAllAsync(clientId)).Select(MarginTradingAccount.Create);
            _cache.UpdateAccountsCache(clientId, accounts);

            await _rabbitMqNotifyService.UserUpdates(false, true, new [] {clientId});
            _console.WriteLine($"send user updates to queue {_marginSettings.RabbitMqQueues.UserUpdates.QueueName}");
        }

        public async Task UpdateBalance(string clientId, string accountId, double amount)
        {
            var updatedAccount = await _repository.UpdateBalanceAsync(clientId, accountId, amount);
            _cache.UpdateBalance(updatedAccount);
        }

        public async Task DeleteAccountAsync(string clientId, string accountId)
        {
            await _repository.DeleteAndSetActiveIfNeededAsync(clientId, accountId);
            await UpdateAccountsCacheAsync(clientId);
        }

        public async Task AddAccountAsync(MarginTradingAccount account)
        {
            await _repository.AddAsync(account);
            await UpdateAccountsCacheAsync(account.ClientId);
        }
    }
}