// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using Lykke.Common.Chaos;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
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
            new AccountContract()
            {
                Id = "testAccount1",
                ClientId = "testClient1",
                TradingConditionId = "testTradingCondition1",
                BaseAssetId = "EUR",
                Balance = 100,
                WithdrawTransferLimit = 0,
                LegalEntity = "Default",
                IsDisabled = false,
                ModificationTimestamp = new DateTime(2018,09,10),
                IsWithdrawalDisabled = false,
                IsDeleted = false,
                AdditionalInfo = "{}"
            },
            new AccountContract()
            {
                Id = "testAccount2",
                ClientId = "testClient1",
                TradingConditionId = "testTradingCondition1",
                BaseAssetId = "EUR",
                Balance = 1000,
                WithdrawTransferLimit = 0,
                LegalEntity = "Default",
                IsDisabled = true,
                ModificationTimestamp = new DateTime(2018,09,10),
                IsWithdrawalDisabled = false,
                IsDeleted = false,
                AdditionalInfo = "{}"
            },
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

            var updatedContract = new AccountContract()
            {
                Id = accountId,
                ClientId = account.ClientId,
                TradingConditionId = updatedTradingConditionId,
                BaseAssetId = account.BaseAssetId,
                Balance = account.Balance,
                WithdrawTransferLimit = updatedWithdrawTransferLimit,
                LegalEntity = account.LegalEntity,
                IsDisabled = isDisabled,
                ModificationTimestamp = account.ModificationTimestamp,
                IsWithdrawalDisabled = account.IsWithdrawalDisabled,
                IsDeleted = false,
                AdditionalInfo = "{}"
            };

            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                updatedContract, AccountChangedEventTypeContract.Updated));

            var resultedAccount = _accountsCacheService.Get(accountId);
            Assert.AreEqual(updatedTradingConditionId, resultedAccount.TradingConditionId);
            Assert.AreEqual(updatedWithdrawTransferLimit, resultedAccount.WithdrawTransferLimit);
            Assert.AreEqual(isDisabled, resultedAccount.IsDisabled);
            Assert.AreEqual(isWithdrawalDisabled, resultedAccount.IsWithdrawalDisabled);
        }

        [Test]
        public void TestAccountUpdateConcurrently_Success()
        {
            //arrange
            var account = Accounts[0];
            var time = DateService.Now().AddMinutes(1);
            
            var accountsProjection = AssertEnv();

            var manualResetEvent = new ManualResetEvent(false);

            //act
            var t1 = new Thread(async () =>
            {

                var accountContract = new AccountContract()
                {
                    Id = account.Id,
                    ClientId = account.ClientId,
                    TradingConditionId = "test",
                    BaseAssetId = "test",
                    Balance = 0,
                    WithdrawTransferLimit = 0,
                    LegalEntity = "test",
                    IsDisabled = false,
                    ModificationTimestamp = time.AddMilliseconds(1),
                    IsWithdrawalDisabled = true,
                    IsDeleted = false,
                    AdditionalInfo = "{}"
                };
                await accountsProjection.Handle(new AccountChangedEvent(time.AddMilliseconds(1), "test",
                    accountContract,
                    AccountChangedEventTypeContract.Updated, null, "operation1"));
                manualResetEvent.WaitOne();
            });
            t1.Start();

            var t2 = new Thread(async () =>
            {
                var accountContract = new AccountContract()
                {
                    Id = account.Id,
                    ClientId = account.ClientId,
                    TradingConditionId = "new",
                    BaseAssetId = "test",
                    Balance = 0,
                    WithdrawTransferLimit = 1,
                    LegalEntity = "test",
                    IsDisabled = true,
                    ModificationTimestamp = time.AddMilliseconds(2),
                    IsWithdrawalDisabled = false,
                    IsDeleted = false,
                    AdditionalInfo = "{}"
                };
                await accountsProjection.Handle(new AccountChangedEvent(time.AddMilliseconds(2), "test",
                    accountContract,
                    AccountChangedEventTypeContract.Updated, null, "operation2"));
                manualResetEvent.WaitOne();
            });
            t2.Start();

            // Make sure both threads are blocked
            while (t1.ThreadState != ThreadState.WaitSleepJoin)
                Thread.Yield();

            while (t2.ThreadState != ThreadState.WaitSleepJoin)
                Thread.Yield();

            // Let them continue
            manualResetEvent.Set();

            // Wait for completion
            t1.Join();
            t2.Join();

            var updatedAccount = _accountsCacheService.Get(account.Id);
            
            //assert
            Assert.AreEqual("new", updatedAccount.TradingConditionId);
            Assert.AreEqual(1, updatedAccount.WithdrawTransferLimit);
            Assert.AreEqual(true, updatedAccount.IsDisabled);
            Assert.AreEqual(false, updatedAccount.IsWithdrawalDisabled);
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
            
            var updatedContract = new AccountContract()
            {
                Id = accountId,
                ClientId = account.ClientId,
                TradingConditionId = updatedTradingConditionId,
                BaseAssetId = account.BaseAssetId,
                Balance = account.Balance,
                WithdrawTransferLimit = updatedWithdrawTransferLimit,
                LegalEntity = account.LegalEntity,
                IsDisabled = isDisabled,
                ModificationTimestamp = account.ModificationTimestamp,
                IsWithdrawalDisabled = account.IsWithdrawalDisabled,
                IsDeleted = false,
                AdditionalInfo = "{}"
            };

            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                updatedContract, AccountChangedEventTypeContract.Updated));

            Assert.AreEqual(1, _logCounter);
        }
        
        [Test]
        [TestCase("testAccount1", 1, AccountBalanceChangeReasonTypeContract.Withdraw, null)]
        [TestCase("testAccount1", 5000, AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL, null)]
        [TestCase("testAccount1", 5000, AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL, "{\"RawTotalPnl\": 0}")]
        [TestCase("testAccount1", 5000, AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL, "{\"RawTotalPnl\": 1}")]
        public async Task TestAccountBalanceUpdate_Success(string accountId, decimal changeAmount,
            AccountBalanceChangeReasonTypeContract balanceChangeReasonType, string auditLog)
        {
            var account = Accounts.Single(x => x.Id == accountId);
            var time = DateService.Now().AddMinutes(1);
            
            var accountsProjection = AssertEnv(accountId: accountId);
            
            var updatedContract = new AccountContract()
            {
                Id = accountId,
                ClientId = account.ClientId,
                TradingConditionId = account.TradingConditionId,
                BaseAssetId = account.BaseAssetId,
                Balance = account.Balance,
                WithdrawTransferLimit = account.WithdrawTransferLimit,
                LegalEntity = account.LegalEntity,
                IsDisabled = account.IsDisabled,
                ModificationTimestamp = account.ModificationTimestamp,
                IsWithdrawalDisabled = account.IsWithdrawalDisabled,
                IsDeleted = false,
                AdditionalInfo = "{}"
            };
            
            await accountsProjection.Handle(new AccountChangedEvent(time, "test",
                updatedContract, AccountChangedEventTypeContract.BalanceUpdated,
                new AccountBalanceChangeContract("test", time, accountId, account.ClientId, changeAmount, 
                    account.Balance + changeAmount, account.WithdrawTransferLimit, "test", balanceChangeReasonType,
                    "test", "Default", auditLog, null, time)));

            var resultedAccount = _accountsCacheService.Get(accountId);
            Assert.AreEqual(account.Balance + changeAmount, resultedAccount.Balance);

            if (balanceChangeReasonType == AccountBalanceChangeReasonTypeContract.Withdraw)
            {
                _accountUpdateServiceMock.Verify(s => s.UnfreezeWithdrawalMargin(accountId, "test"), Times.Once);
            }

            if (balanceChangeReasonType == AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL)
            {
                var metadata = auditLog?.DeserializeJson<UnrealizedPnlMetadataContract>();

                if (metadata == null || metadata.RawTotalPnl == 0)
                {
                    _fakePosition.Verify(s => s.ChargePnL("test", changeAmount), Times.Once);
                }
                else
                {
                    _fakePosition.Verify(s => s.SetChargedPnL("test", metadata.RawTotalPnl), Times.Once);
                }
            }
            
            _accountBalanceChangedEventChannelMock.Verify(s => s.SendEvent(It.IsAny<object>(), 
                It.IsAny<AccountBalanceChangedEventArgs>()), Times.Once);
        }

        private AccountsProjection AssertEnv(string accountId = null, string failMessage = null)
        {
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
            _fakePosition.SetupProperty(s => s.FxAssetPairId, "test");
            _fakePosition.SetupProperty(s => s.ChargePnlOperations, new HashSet<string>());
            _fakePosition.Setup(s => s.ChargePnL(It.Is<string>(x => x == "test"), It.IsAny<decimal>()));
            _ordersCache.Positions.Add(_fakePosition.Object);

            return new AccountsProjection(_accountsCacheService,
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