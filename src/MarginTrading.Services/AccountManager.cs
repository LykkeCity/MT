using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.AzureRepositories.Reports;
using MarginTrading.Core;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class AccountManager: TimerPeriod
    {
        private readonly AccountsCacheService _accountsCacheService;
        private readonly IMarginTradingAccountsRepository _repository;
        private readonly IConsole _console;
        private readonly MarginSettings _marginSettings;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IAccountGroupCacheService _accountGroupCacheService;
        private readonly IMarginTradingAccountsRepository _accountsRepository;
        private readonly ITradingConditionsCacheService _tradingConditionsCacheService;
        private readonly ILog _log;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly IAccountsStatsReportsRepository _accountsStatsReportsRepository;
        private readonly IAccountsReportsRepository _accountsReportsRepository;
        private readonly IMarginTradingAccountStatsRepository _statsRepository;


        public AccountManager(AccountsCacheService accountsCacheService,
            IMarginTradingAccountsRepository repository,
            IConsole console,
            MarginSettings marginSettings,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAccountGroupCacheService accountGroupCacheService,
            IMarginTradingAccountsRepository accountsRepository,
            ITradingConditionsCacheService tradingConditionsCacheService,
            ILog log,
            IClientNotifyService clientNotifyService,
            IAccountsStatsReportsRepository accountsStatsReportsRepository,
            IAccountsReportsRepository accountsReportsRepository,
            IMarginTradingAccountStatsRepository statsRepository)
            : base(nameof(AccountManager), 10000, log)
        {
            _accountsCacheService = accountsCacheService;
            _repository = repository;
            _console = console;
            _marginSettings = marginSettings;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _accountGroupCacheService = accountGroupCacheService;
            _accountsRepository = accountsRepository;
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _log = log;
            _clientNotifyService = clientNotifyService;
            _accountsStatsReportsRepository = accountsStatsReportsRepository;
            _accountsReportsRepository = accountsReportsRepository;
            _statsRepository = statsRepository;
        }

        public override Task Execute()
        {
            var accounts = _accountsCacheService.GetAll();
            var statsWritingTask = WriteAccountsStats(accounts);
            var writeAccountsStatsReportsTask = WriteAccountsStatsReports(accounts);
            return Task.WhenAll(statsWritingTask, writeAccountsStatsReportsTask);
        }

        private Task WriteAccountsStats(IReadOnlyList<MarginTradingAccount> accounts)
        {
            var stats = accounts
                .Select(a => new MarginTradingAccountStats
                {
                    AccountId = a.Id,
                    BaseAssetId = a.BaseAssetId,
                    MarginCall = a.GetMarginCall(),
                    StopOut = a.GetStopOut(),
                    TotalCapital = a.GetTotalCapital(),
                    FreeMargin = a.GetFreeMargin(),
                    MarginAvailable = a.GetMarginAvailable(),
                    UsedMargin = a.GetUsedMargin(),
                    MarginInit = a.GetMarginInit(),
                    PnL = a.GetPnl(),
                    OpenPositionsCount = a.GetOpenPositionsCount(),
                    MarginUsageLevel = a.GetMarginUsageLevel(),
                });
            return _statsRepository.InsertOrReplaceBatchAsync(stats);
        }



        private Task WriteAccountsStatsReports(IReadOnlyList<MarginTradingAccount> accounts)
        {
            var stats = accounts.Select(a => new AccountsStatReport
            {
                AccountId = a.Id,
                ClientId = a.ClientId,
                TradingConditionId = a.TradingConditionId,
                BaseAssetId = a.BaseAssetId,
                Balance = a.Balance,
                WithdrawTransferLimit = a.WithdrawTransferLimit,
                MarginCall = a.GetMarginCall(),
                StopOut = a.GetStopOut(),
                TotalCapital = a.GetTotalCapital(),
                FreeMargin = a.GetFreeMargin(),
                MarginAvailable = a.GetMarginAvailable(),
                UsedMargin = a.GetUsedMargin(),
                MarginInit = a.GetMarginInit(),
                PnL = a.GetPnl(),
                OpenPositionsCount = a.GetOpenPositionsCount(),
                MarginUsageLevel = a.GetMarginUsageLevel(),
                IsLive = _marginSettings.IsLive
            });

            return _accountsStatsReportsRepository.InsertOrReplaceBatchAsync(stats);
        }

        private Task WriteAccountsReports(IEnumerable<MarginTradingAccount> accounts)
        {
            var reports = accounts.Select(a => new AccountsReport
            {
                TakerAccountId = a.Id,
                TakerCounterpartyId = a.ClientId,
                BaseAssetId = a.BaseAssetId,
                IsLive = _marginSettings.IsLive
            });

            return _accountsReportsRepository.InsertOrReplaceBatchAsync(reports);
        }

        public override void Start()
        {
            var accounts = _repository.GetAllAsync().GetAwaiter().GetResult()
                .Select(MarginTradingAccount.Create).GroupBy(x => x.ClientId).ToDictionary(x => x.Key, x => x.ToArray());

            _accountsCacheService.InitAccountsCache(accounts);
            _console.WriteLine($"InitAccountsCache (clients count:{accounts.Count})");

            base.Start();
        }

        private async Task ProcessAccountsSetChange(string clientId, IReadOnlyList<MarginTradingAccount> allClientsAccounts = null)
        {
            if (allClientsAccounts == null)
            {
                allClientsAccounts = (await _repository.GetAllAsync(clientId)).Select(MarginTradingAccount.Create).ToList();
            }

            _accountsCacheService.UpdateAccountsCache(clientId, allClientsAccounts);
            await Task.WhenAll(
                WriteAccountsReports(allClientsAccounts),
                _rabbitMqNotifyService.UserUpdates(false, true, new[] { clientId }));
        }

        public async Task UpdateBalanceAsync(string clientId, string accountId, double amount, AccountHistoryType historyType, string comment, bool changeTransferLimit = false)
        {
            if (historyType == AccountHistoryType.Deposit && changeTransferLimit)
            {
                await CheckDepositLimits(clientId, accountId, amount);
            }

            if (changeTransferLimit)
            {
                await CheckTransferLimits(clientId, accountId, amount);
            }

            var updatedAccount = await _repository.UpdateBalanceAsync(clientId, accountId, amount, changeTransferLimit);
            _accountsCacheService.UpdateBalance(updatedAccount);

            _clientNotifyService.NotifyAccountChanged(updatedAccount);

            await _rabbitMqNotifyService.AccountHistory(accountId, clientId, amount, updatedAccount.Balance, updatedAccount.WithdrawTransferLimit, historyType, comment);
        }

        private async Task CheckDepositLimits(string clientId, string accountId, double amount)
        {
            var account = await _repository.GetAsync(clientId, accountId);

            //limit can not be more then max after deposit
            if (amount > 0)
            {
                var accountGroup =
                    _accountGroupCacheService.GetAccountGroup(account.TradingConditionId, account.BaseAssetId);

                if (accountGroup.DepositTransferLimit > 0 && accountGroup.DepositTransferLimit < account.Balance + amount)
                {
                    throw new Exception(
                        $"Margin Trading is in beta testing. The cash-ins are temporarily limited when Total Capital exceeds {accountGroup.DepositTransferLimit} {accountGroup.BaseAssetId}. Thank you for using Lykke Margin Trading, the limit will be cancelled soon!");
                }
            }
        }

        private async Task CheckTransferLimits(string clientId, string accountId, double amount)
        {
            var account = await _repository.GetAsync(clientId, accountId);

            //withdraw can not be more then limit
            if (amount < 0 && account.WithdrawTransferLimit < Math.Abs(amount))
            {
                throw new Exception(
                    $"Can not transfer {Math.Abs(amount)}. Current limit is {account.WithdrawTransferLimit}");
            }
        }

        public async Task DeleteAccountAsync(string clientId, string accountId)
        {
            await _repository.DeleteAsync(clientId, accountId);
            await ProcessAccountsSetChange(clientId);
        }

        //TODO: close/remove all orders
        public async Task ResetAccountAsync(string clientId, string accountId)
        {
            var account = _accountsCacheService.Get(clientId, accountId);

            await UpdateBalanceAsync(clientId, accountId, -account.Balance, AccountHistoryType.Reset,
                "Reset account");

            await UpdateBalanceAsync(clientId, accountId, LykkeConstants.DefaultDemoBalance, AccountHistoryType.Deposit,
                "Initial deposit");
        }

        public async Task AddAccountAsync(string clientId, string baseAssetId, string tradingConditionId)
        {
            var account = CreateAccount(clientId, baseAssetId, tradingConditionId);
            await _repository.AddAsync(account);
            await ProcessAccountsSetChange(account.ClientId);
        }

        public async Task<MarginTradingAccount[]> CreateDefaultAccounts(string clientId, string tradingConditionsId = null)
        {
            var existingAccounts = (await _accountsRepository.GetAllAsync(clientId)).ToList();

            if (existingAccounts.Any())
            {
                var accounts = existingAccounts.Select(MarginTradingAccount.Create).ToArray();
                _accountsCacheService.UpdateAccountsCache(clientId, accounts);
                return accounts;
            }

            if (string.IsNullOrEmpty(tradingConditionsId))
                tradingConditionsId = GetTradingConditions();

            var baseAssets = GetBaseAssets(tradingConditionsId);

            var newAccounts = new List<MarginTradingAccount>();

            foreach (var baseAsset in baseAssets)
            {
                try
                {
                    var account = CreateAccount(clientId, baseAsset, tradingConditionsId);
                    await _repository.AddAsync(account);
                    newAccounts.Add(account);
                }
                catch (Exception e)
                {
                    await _log.WriteErrorAsync(nameof(AccountManager), "Create default accounts",
                        $"clientId={clientId}, tradingConditionsId={tradingConditionsId}", e);
                }
            }

            await ProcessAccountsSetChange(clientId, newAccounts);

            return newAccounts.ToArray();
        }

        #region Helpers

        private string[] GetBaseAssets(string tradingConditionsId)
        {
            var accountGroups =
                _accountGroupCacheService.GetAllAccountGroups().Where(g => g.TradingConditionId == tradingConditionsId);
            var baseAssets = accountGroups.Select(g => g.BaseAssetId).Distinct().ToArray();

            if (!baseAssets.Any())
                throw new Exception(
                    $"No account groups found for trading conditions {tradingConditionsId}");

            return baseAssets;
        }

        private string GetTradingConditions()
        {
            //use default trading conditions for demo
            if (!_marginSettings.IsLive)
            {
                var tradingConditions = _tradingConditionsCacheService.GetAllTradingConditions();
                var defaultConditions = tradingConditions.FirstOrDefault(item => item.IsDefault);

                if (defaultConditions == null)
                    throw new Exception("No default trading conditions set for demo");
                else
                    return defaultConditions.Id;
            }
            else
            {
                throw new Exception("No trading conditions found");
            }
        }

        private MarginTradingAccount CreateAccount(string clientId, string baseAssetId, string tradingConditionId)
        {
            var id = $"{(_marginSettings.IsLive ? string.Empty : _marginSettings.DemoAccountIdPrefix)}{Guid.NewGuid():N}";
            var initialBalance = _marginSettings.IsLive ? 0 : LykkeConstants.DefaultDemoBalance;

            return new MarginTradingAccount
            {
                Id = id,
                BaseAssetId = baseAssetId,
                ClientId = clientId,
                Balance = initialBalance,
                TradingConditionId = tradingConditionId
            };
        }

        #endregion
    }
}