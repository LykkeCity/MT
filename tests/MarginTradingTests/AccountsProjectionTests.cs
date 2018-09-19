using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Common.Chaos;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow;
using MarginTrading.Common.Services;
using MarginTrading.SqlRepositories.Repositories;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    [NonParallelizable]
    public class AccountsProjectionTests
    {
        private IAccountsCacheService _accountsCacheService;
        //private Mock<IAccountsCacheService> _accountCacheServiceMock;
        private Mock<IClientNotifyService> _clientNotifyServiceMock;
        private Mock<IEventChannel<AccountBalanceChangedEventArgs>> _accountBalanceChangedEventChannelMock;
        private Mock<IAccountUpdateService> _updateAccountServiceMock;
        private static readonly IDateService DateService = new DateService();
        private static readonly IConvertService ConvertService = new ConvertService();
        private Mock<IOperationExecutionInfoRepository> _operationExecutionInfoRepositoryMock;
        private OrdersCache _ordersCache;
        private Mock<ILog> _logMock;

        private static readonly AccountContract[] Accounts =
        {
            new AccountContract( //already existed
                id: "testAccount1",
                clientId: "testClient1",
                tradingConditionId: "testTradingCondition1",
                baseAssetId: "EUR",
                balance: 100,
                withdrawTransferLimit: 0,
                legalEntity: "Default",
                isDisabled: false,
                modificationTimestamp: new DateTime(2018, 09, 13),
                isWithdrawalDisabled: false
            ),
            new AccountContract(
                id: "testAccount2",
                clientId: "testClient1",
                tradingConditionId: "testTradingCondition1",
                baseAssetId: "EUR",
                balance: 1000,
                withdrawTransferLimit: 0,
                legalEntity: "Default",
                isDisabled: true,
                modificationTimestamp: new DateTime(2018, 09, 13),
                isWithdrawalDisabled: false
            )
        };

        [Test]
        public async Task TestAccountCreation()
        {
            var account = Accounts[1];
            var time = DateService.Now();
            
            var accountsProjection = AssertEnv();

            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                account, AccountChangedEventTypeContract.Created));

            var createdAccount = _accountsCacheService.TryGet(account.Id);
            Assert.True(createdAccount != null);
            Assert.AreEqual(account.Id, createdAccount.Id);
            Assert.AreEqual(account.Balance, createdAccount.Balance);
            Assert.AreEqual(account.TradingConditionId, createdAccount.TradingConditionId);
        }

        [Test]
        [TestCase("testAccount1", "default", 0, false, false)]
        [TestCase("testAccount1", "test", 1, true, false)]
        public async Task TestAccountUpdate_Success(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled)
        {
            var account = Accounts.Single(x => x.Id == accountId);
            var time = DateService.Now().AddMinutes(1);
            
            var accountsProjection = AssertEnv();

            var updatedContract = new AccountContract(accountId, account.ClientId, updatedTradingConditionId,
                account.BaseAssetId, account.Balance, updatedWithdrawTransferLimit, account.LegalEntity,
                isDisabled, account.ModificationTimestamp, account.IsWithdrawalDisabled);
            
            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                updatedContract, AccountChangedEventTypeContract.Updated));

            var resultedAccount = _accountsCacheService.Get(accountId);
            Assert.AreEqual(updatedTradingConditionId, resultedAccount.TradingConditionId);
            Assert.AreEqual(updatedWithdrawTransferLimit, resultedAccount.WithdrawTransferLimit);
            Assert.AreEqual(isDisabled, resultedAccount.IsDisabled);
            Assert.AreEqual(isWithdrawalDisabled, resultedAccount.IsWithdrawalDisabled);
            
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.IsAny<IMarginTradingAccount>()), Times.Once);
        }

        [Test]
        [TestCase("testAccount2", "default", 0, false, false)]
        [TestCase("testAccount1", "test", 1, true, false)]
        public async Task TestAccountUpdate_Fail(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled)
        {
            var account = Accounts.Single(x => x.Id == accountId);
            var time = DateService.Now();
            
            var accountsProjection = AssertEnv();

            var updatedContract = new AccountContract(accountId, account.ClientId, updatedTradingConditionId,
                account.BaseAssetId, account.Balance, updatedWithdrawTransferLimit, account.LegalEntity,
                isDisabled, account.ModificationTimestamp, account.IsWithdrawalDisabled);
            
            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                updatedContract, AccountChangedEventTypeContract.Updated));

            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.IsAny<IMarginTradingAccount>()), Times.Never);
        }
        
        [Test]
        [TestCase("testAccount1", 1, AccountBalanceChangeReasonTypeContract.Withdraw)]
        [TestCase("testAccount1", 5000, AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL)]
        public async Task TestAccountBalanceUpdate_Success(string accountId, decimal balance,
            AccountBalanceChangeReasonTypeContract balanceChangeReasonType)
        {
            var account = Accounts.Single(x => x.Id == accountId);
            var time = DateService.Now().AddMinutes(1);
            
            var accountsProjection = AssertEnv();

            var updatedContract = new AccountContract(accountId, account.ClientId, account.TradingConditionId,
                account.BaseAssetId, balance, account.WithdrawTransferLimit, account.LegalEntity,
                account.IsDisabled, account.ModificationTimestamp, account.IsWithdrawalDisabled);
            
            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                updatedContract, AccountChangedEventTypeContract.BalanceUpdated,
                new AccountBalanceChangeContract("1", time, accountId, account.ClientId, 0, balance,
                    account.WithdrawTransferLimit, "test", balanceChangeReasonType,
                    "test", "Default", null, null, time)));

            var resultedAccount = _accountsCacheService.Get(accountId);
            Assert.AreEqual(balance, resultedAccount.Balance);

            if (balanceChangeReasonType == AccountBalanceChangeReasonTypeContract.Withdraw)
            {
                _updateAccountServiceMock.Verify(s => s.UnfreezeWithdrawalMargin(accountId, "1"), Times.Once);
            }
            
            _accountBalanceChangedEventChannelMock.Verify(s => s.SendEvent(It.IsAny<object>(), 
                It.IsAny<AccountBalanceChangedEventArgs>()), Times.Once);
        }

        private AccountsProjection AssertEnv()
        {
            _accountsCacheService = new AccountsCacheService(DateService, new EmptyLog());
            
            _accountsCacheService.TryAddNew(Convert(Accounts[0]));

            _clientNotifyServiceMock = new Mock<IClientNotifyService>();
            _accountBalanceChangedEventChannelMock = new Mock<IEventChannel<AccountBalanceChangedEventArgs>>();
            _updateAccountServiceMock = new Mock<IAccountUpdateService>();
            _operationExecutionInfoRepositoryMock = new Mock<IOperationExecutionInfoRepository>();
            _operationExecutionInfoRepositoryMock.Setup(s => s.GetOrAddAsync(It.IsIn("AccountsProjection"),
                    It.IsAny<string>(), It.IsAny<Func<IOperationExecutionInfo<OperationData>>>()))
                .ReturnsAsync(new OperationExecutionInfo<OperationData>(
                    operationName: "AccountsProjection",
                    id: Guid.NewGuid().ToString(),
                    lastModified: DateService.Now(),
                    data: new OperationData {State = OperationState.Initiated}
                ));
            _ordersCache = new OrdersCache();
            _logMock = new Mock<ILog>();
            
            return new AccountsProjection(_accountsCacheService, _clientNotifyServiceMock.Object,
                _accountBalanceChangedEventChannelMock.Object, ConvertService, _updateAccountServiceMock.Object, 
                DateService, _operationExecutionInfoRepositoryMock.Object, Mock.Of<IChaosKitty>(), 
                _ordersCache, _logMock.Object);
        }
        
        private static MarginTradingAccount Convert(AccountContract accountContract)
        {
            return ConvertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source)
                    .ForMember(d => d.LastUpdateTime,
                        a => a.MapFrom(x =>
                            x.ModificationTimestamp)));
        }
    }
}