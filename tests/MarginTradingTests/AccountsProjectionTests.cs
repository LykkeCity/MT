using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Common.Chaos;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
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
    public class AccountsProjectionTests
    {
        private IAccountsCacheService _accountsCacheService;
        //private Mock<IAccountsCacheService> _accountCacheServiceMock;
        private Mock<IClientNotifyService> _clientNotifyServiceMock;
        private Mock<IEventChannel<AccountBalanceChangedEventArgs>> _accountBalanceChangedEventChannelMock;
        private Mock<IAccountUpdateService> _accountUpdateServiceMock;
        private static readonly IDateService DateService = new DateService();
        private static readonly IConvertService ConvertService = new ConvertService();
        private Mock<IOperationExecutionInfoRepository> _operationExecutionInfoRepositoryMock;
        private OrdersCache _ordersCache;
        private Mock<ILog> _logMock;
        private Mock<Position> _fakePosition;
        
        private int _logCounter = 0;

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
        public async Task TestAccountUpdateConcurrently_Success()
        {
            var account = Accounts[0];
            var time = DateService.Now().AddMinutes(1);
            
            var accountsProjection = AssertEnv();
            
            var updateTask1 = accountsProjection.Handle(new AccountChangedEvent(time, "test", 
                new AccountContract(account.Id, account.ClientId, "test", "test", 0, 0, "test", false, time.AddMilliseconds(1), true), 
                AccountChangedEventTypeContract.Updated, null, "operation1"));
            var updateTask2 = accountsProjection.Handle(new AccountChangedEvent(time, "test", 
                new AccountContract(account.Id, account.ClientId, "new", "test", 0, 1, "test", true, time.AddMilliseconds(2), false), 
                AccountChangedEventTypeContract.Updated, null, "operation2"));

            await Task.WhenAll(updateTask1, updateTask2);

            var updatedAccount = _accountsCacheService.Get(account.Id);
            
            Assert.AreEqual("new", updatedAccount.TradingConditionId);
            Assert.AreEqual(1, updatedAccount.WithdrawTransferLimit);
            Assert.AreEqual(true, updatedAccount.IsDisabled);
            Assert.AreEqual(false, updatedAccount.IsWithdrawalDisabled);
            
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.IsAny<IMarginTradingAccount>()), Times.Exactly(2));
        }

        [Test]
        [TestCase("testAccount2", "default", 0, false, false, "Account with id testAccount2 was not found")]
        [TestCase("testAccount1", "test", 1, true, false, "Account with id testAccount1 is in newer state then the event")]
        public async Task TestAccountUpdate_Fail(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled, string failMessage)
        {
            var account = Accounts.Single(x => x.Id == accountId);
            var time = DateService.Now();
            
            var accountsProjection = AssertEnv(failMessage: failMessage);

            var updatedContract = new AccountContract(accountId, account.ClientId, updatedTradingConditionId,
                account.BaseAssetId, account.Balance, updatedWithdrawTransferLimit, account.LegalEntity,
                isDisabled, account.ModificationTimestamp, account.IsWithdrawalDisabled);
            
            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                updatedContract, AccountChangedEventTypeContract.Updated));

            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.IsAny<IMarginTradingAccount>()), Times.Never);
            Assert.AreEqual(1, _logCounter);
        }
        
        [Test]
        [TestCase("testAccount1", 1, AccountBalanceChangeReasonTypeContract.Withdraw)]
        [TestCase("testAccount1", 5000, AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL)]
        public async Task TestAccountBalanceUpdate_Success(string accountId, decimal changeAmount,
            AccountBalanceChangeReasonTypeContract balanceChangeReasonType)
        {
            var account = Accounts.Single(x => x.Id == accountId);
            var time = DateService.Now().AddMinutes(1);
            
            var accountsProjection = AssertEnv(accountId: accountId);

            var updatedContract = new AccountContract(accountId, account.ClientId, account.TradingConditionId,
                account.BaseAssetId, changeAmount, account.WithdrawTransferLimit, account.LegalEntity,
                account.IsDisabled, account.ModificationTimestamp, account.IsWithdrawalDisabled);
            
            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                updatedContract, AccountChangedEventTypeContract.BalanceUpdated,
                new AccountBalanceChangeContract("test", time, accountId, account.ClientId, changeAmount, 
                    account.Balance + changeAmount, account.WithdrawTransferLimit, "test", balanceChangeReasonType,
                    "test", "Default", null, null, time)));

            var resultedAccount = _accountsCacheService.Get(accountId);
            Assert.AreEqual(account.Balance + changeAmount, resultedAccount.Balance);

            if (balanceChangeReasonType == AccountBalanceChangeReasonTypeContract.Withdraw)
            {
                _accountUpdateServiceMock.Verify(s => s.UnfreezeWithdrawalMargin(accountId, "test"), Times.Once);
            }

            if (balanceChangeReasonType == AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL)
            {
                _fakePosition.Verify(s => s.ChargePnL("test", changeAmount), Times.Once);
            }
            
            _accountBalanceChangedEventChannelMock.Verify(s => s.SendEvent(It.IsAny<object>(), 
                It.IsAny<AccountBalanceChangedEventArgs>()), Times.Once);
        }

        private AccountsProjection AssertEnv(string accountId = null, string failMessage = null)
        {
            _clientNotifyServiceMock = new Mock<IClientNotifyService>();
            _accountBalanceChangedEventChannelMock = new Mock<IEventChannel<AccountBalanceChangedEventArgs>>();
            _accountUpdateServiceMock = new Mock<IAccountUpdateService>();
            _accountUpdateServiceMock.Setup(s => s.UnfreezeWithdrawalMargin(It.Is<string>(x => x == accountId), "test"))
                .Returns(Task.CompletedTask);
            _operationExecutionInfoRepositoryMock = new Mock<IOperationExecutionInfoRepository>();
            _operationExecutionInfoRepositoryMock.Setup(s => s.GetOrAddAsync(It.Is<string>(x => x == "AccountsProjection"),
                    It.IsAny<string>(), It.IsAny<Func<IOperationExecutionInfo<OperationData>>>()))
                .ReturnsAsync(() => new OperationExecutionInfo<OperationData>(
                    operationName: "AccountsProjection",
                    id: Guid.NewGuid().ToString(),
                    lastModified: DateService.Now(),
                    data: new OperationData {State = OperationState.Initiated}
                ));
            
            _logMock = new Mock<ILog>();
            if (failMessage != null)
            {
                _logCounter = 0;
                _logMock.Setup(s => s.WriteInfoAsync(It.IsAny<string>(), It.IsAny<string>(), 
                    It.Is<string>(x => x == failMessage), It.IsAny<DateTime?>()))
                    .Callback(() => _logCounter++).Returns(Task.CompletedTask);
                _logMock.Setup(s => s.WriteWarningAsync(It.IsAny<string>(), It.IsAny<string>(), 
                    It.Is<string>(x => x == failMessage), It.IsAny<DateTime?>()))
                    .Callback(() => _logCounter++).Returns(Task.CompletedTask);
            }
            
            _accountsCacheService = new AccountsCacheService(DateService, _logMock.Object);
            _accountsCacheService.TryAddNew(Convert(Accounts[0]));
            MtServiceLocator.AccountsCacheService = _accountsCacheService;
            
            _ordersCache = new OrdersCache();
            _fakePosition = new Mock<Position>();
            _fakePosition.SetupProperty(s => s.Id, "test");
            _fakePosition.SetupProperty(s => s.AccountId, Accounts[0].Id);
            _fakePosition.SetupProperty(s => s.AssetPairId, "test");
            _fakePosition.SetupProperty(s => s.ChargePnlOperations, new HashSet<string>());
            _fakePosition.Setup(s => s.ChargePnL(It.Is<string>(x => x == "test"), It.IsAny<decimal>()));
            _ordersCache.Positions.Add(_fakePosition.Object);

            return new AccountsProjection(_accountsCacheService, _clientNotifyServiceMock.Object,
                _accountBalanceChangedEventChannelMock.Object, ConvertService, _accountUpdateServiceMock.Object, 
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