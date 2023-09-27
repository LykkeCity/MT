// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Workflow;
using MarginTrading.Common.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests.WorkflowTests
{
    [TestFixture]
    public class WithdrawalCommandsHandlerTests : BaseTests
    {
        private IAccountsProvider _accountsProvider;

        [SetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _accountsProvider = Container.Resolve<IAccountsProvider>();
        }

        /// <summary>
        /// Three withdrawal operations are run in parallel.
        /// With total balance of 1000 eur, and the withdrawal amount of 700 each, only one operation should succeed
        /// So the handler should send 1 <see cref="AmountForWithdrawalFrozenEvent"/> event and 2 <see cref="AmountForWithdrawalFreezeFailedEvent"/> events
        /// </summary>
        [Test]
        [Repeat(20)]
        public async Task Handle_MultipleWithdrawals_OnlyOneSucceeds()
        {
            // Arrange
            var dateService = new Mock<IDateService>();
            var chaosKitty = new Mock<IChaosKitty>();
            var operationExecutionInfoRepository = new Mock<IOperationExecutionInfoRepository>();
            var logger = new Mock<ILogger<WithdrawalCommandsHandler>>();
            var eventPublisher = new Mock<IEventPublisher>();

            var accountId = Accounts[1].Id;
            var amount = 700;

            operationExecutionInfoRepository.Setup(x => x.GetOrAddAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<IOperationExecutionInfo<WithdrawalFreezeOperationData>>>()
                ))
                .ReturnsAsync(() =>
                {
                    var info = new OperationExecutionInfo<WithdrawalFreezeOperationData>(
                        "operation",
                        "id",
                        DateTime.UtcNow,
                        new WithdrawalFreezeOperationData()
                        {
                            Amount = amount,
                            AccountId = accountId,
                            State = OperationState.Initiated,
                        }
                    );

                    return (info, true);
                });

            var withdrawalCommandsHandler = new WithdrawalCommandsHandler(
                dateService.Object,
                chaosKitty.Object,
                operationExecutionInfoRepository.Object,
                logger.Object,
                _accountsProvider
            );

            var command1 = new FreezeAmountForWithdrawalCommand("command1",
                DateTime.UtcNow,
                accountId,
                amount,
                "withdrawal");
            var command2 = new FreezeAmountForWithdrawalCommand("command2",
                DateTime.UtcNow,
                accountId,
                amount,
                "withdrawal");
            var command3 = new FreezeAmountForWithdrawalCommand("command3",
                DateTime.UtcNow,
                accountId,
                amount,
                "withdrawal");

            // new Task() ensures that tasks are not started immediately
            var task1 = new Task(() => withdrawalCommandsHandler.Handle(command1, eventPublisher.Object));
            var task2 = new Task(() => withdrawalCommandsHandler.Handle(command2, eventPublisher.Object));
            var task3 = new Task(() => withdrawalCommandsHandler.Handle(command3, eventPublisher.Object));

            List<Task> tasks = new List<Task>()
            {
                task1, task2, task3
            };

            // Action
            Parallel.ForEach(tasks, x => x.Start());
            await Task.WhenAll(tasks);

            // Assert
            eventPublisher.Verify(x => x.PublishEvent(It.IsAny<AmountForWithdrawalFrozenEvent>()),
                Times.Once);

            eventPublisher.Verify(x => x.PublishEvent(It.IsAny<AmountForWithdrawalFreezeFailedEvent>()),
                Times.Exactly(2));
        }
    }
}