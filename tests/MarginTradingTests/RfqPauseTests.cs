// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Snow.Common;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation;
using MarginTrading.Common.Services;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class RfqPauseTests
    {
        private Mock<IOperationExecutionPauseRepository> _repositoryPauseMock;
        private Mock<IOperationExecutionInfoRepository> _repositoryInfoMock;
        private Mock<IDateService> _dateServiceMock;
        private Mock<ICqrsSender> _cqrsSenderMock;
        private Mock<ILog> _logMock;

        private static readonly object[] PauseStatesCannotCancelIn =
        {
            new object[] { PauseState.Cancelled },
            new object[] { PauseState.Pending },
            new object[] { PauseState.PendingCancellation }
        };

        [SetUp]
        public void Setup()
        {
            _repositoryPauseMock = new Mock<IOperationExecutionPauseRepository>();
            _repositoryInfoMock = new Mock<IOperationExecutionInfoRepository>();
            _dateServiceMock = new Mock<IDateService>();
            _cqrsSenderMock = new Mock<ICqrsSender>();
            _logMock = new Mock<ILog>();
        }

        [Test]
        public async Task AddPause_When_ThereIsAlready_Pending_Or_Active_One_Returns_Error()
        {
            // configure repository to always find a pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<Func<Pause, bool>>()))
                .ReturnsAsync(new[] { GetPause() });
            
            var pauseService = GetSut();

            var errorCode = await pauseService.AddAsync("whatever", PauseSource.Manual, "whatever");
            
            Assert.AreEqual(RfqPauseErrorCode.AlreadyExists, errorCode);
        }

        [Test]
        public async Task AddPause_When_ThereIsNo_ExecutionInfo_Returns_Error()
        {
            // configure repository to not find execution info
            _repositoryInfoMock
                .Setup(x => x.GetAsync<SpecialLiquidationOperationData>(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((IOperationExecutionInfo<SpecialLiquidationOperationData>)null);

            var pauseService = GetSut();

            var errorCode = await pauseService.AddAsync("whatever", PauseSource.Manual, "whatever");

            Assert.AreEqual(RfqPauseErrorCode.NotFound, errorCode);
        }

        [Test]
        public async Task AddPause_When_OperationState_IsNotAllowed_Returns_Error()
        {
            var executionInfo = GetExecutionInfoWithState(GetRandomOperationState(false));
            
            // configure repository to always return operation info with not allowed state for pause
            _repositoryInfoMock
                .Setup(x => x.GetAsync<SpecialLiquidationOperationData>(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(executionInfo);

            var pauseService = GetSut();

            var errorCode = await pauseService.AddAsync("whatever", PauseSource.Manual, "whatever");
            
            Assert.AreEqual(RfqPauseErrorCode.InvalidOperationState, errorCode);
        }

        [Test]
        public async Task Acknowledge_When_Active_Pause_Exists_Returns_Success()
        {
            // configure repository to return active pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    "active",
                    SpecialLiquidationSaga.Name,
                    It.IsAny<Func<Pause, bool>>()))
                .ReturnsAsync(new[] { GetPause(PauseState.Active) });
            
            var pauseService = GetSut();

            var acknowledged = await pauseService.AcknowledgeAsync("active");
            
            Assert.IsTrue(acknowledged);
        }

        [Test]
        public async Task Acknowledge_When_Pending_Pause_Exists_Updates_It_And_Returns_Success()
        {
            // configure repository to not return active pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    "pending",
                    SpecialLiquidationSaga.Name,
                    RfqPauseService.ActivePredicate))
                .ReturnsAsync(Enumerable.Empty<Pause>());

            
            // configure repository to return pending pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    "pending",
                    SpecialLiquidationSaga.Name,
                    RfqPauseService.PendingPredicate))
                .ReturnsAsync(new[] { GetPersistedPause(PauseState.Pending) });

            // configure repository to succeed when update is called
            _repositoryPauseMock
                .Setup(x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<PauseState>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<Initiator>(),
                    It.IsAny<PauseCancellationSource?>()))
                .ReturnsAsync(true);

            var pauseService = GetSut();

            var acknowledged = await pauseService.AcknowledgeAsync("pending");

            _repositoryPauseMock.Verify(x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<DateTime>(),
                    PauseState.Active,
                    null,
                    null,
                    null,
                    null),
                Times.Once);
            
            Assert.IsTrue(acknowledged);
        }

        [Test]
        public async Task Acknowledge_When_No_Pause_Exists_Returns_Failure()
        {
            // configure repository to not return any pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    It.IsAny<string>(),
                    SpecialLiquidationSaga.Name,
                    It.IsAny<Func<Pause, bool>>()))
                .ReturnsAsync(Enumerable.Empty<Pause>());
            
            var pauseService = GetSut();

            var acknowledged = await pauseService.AcknowledgeAsync("pending");
            
            Assert.IsFalse(acknowledged);
        }

        [Test]
        public async Task StopPending_When_Pending_Exists_Updates_State_To_Cancelled()
        {
            // configure repository to return pending pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    "pending",
                    SpecialLiquidationSaga.Name,
                    RfqPauseService.PendingPredicate))
                .ReturnsAsync(new[] { GetPersistedPause(PauseState.Pending) });

            var pauseService = GetSut();

            await pauseService.StopPendingAsync("pending", PauseCancellationSource.Manual, "whatever");

            _repositoryPauseMock.Verify(x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<DateTime?>(),
                    PauseState.Cancelled,
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<Initiator>(),
                    It.IsAny<PauseCancellationSource?>()),
                Times.Once);
        }

        [Test]
        public async Task AcknowledgeCancellation_When_PendingCancellation_Exists_Updates_State_To_Cancelled()
        {
            // configure repository to return pending cancellation pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    "pending cancellation",
                    SpecialLiquidationSaga.Name,
                    RfqPauseService.PendingCancellationPredicate))
                .ReturnsAsync(new[] { GetPersistedPause(PauseState.PendingCancellation) });
            
            // configure repository to succeed when update is called
            _repositoryPauseMock
                .Setup(x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<PauseState>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<Initiator>(),
                    It.IsAny<PauseCancellationSource?>()))
                .ReturnsAsync(true);

            var pauseService = GetSut();

            var acknowledged = await pauseService.AcknowledgeCancellationAsync("pending cancellation");
            
            _repositoryPauseMock.Verify(x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<DateTime?>(),
                    PauseState.Cancelled,
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<Initiator>(),
                    It.IsAny<PauseCancellationSource?>()),
                Times.Once);
            
            Assert.IsTrue(acknowledged);
        }

        [Test]
        public async Task AcknowledgeCancellation_When_NoPause_Exists_Returns_Failure()
        {
            // configure repository to not return any pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    It.IsAny<string>(),
                    SpecialLiquidationSaga.Name,
                    It.IsAny<Func<Pause, bool>>()))
                .ReturnsAsync(Enumerable.Empty<Pause>());
            
            var pauseService = GetSut();

            var acknowledged = await pauseService.AcknowledgeCancellationAsync("pending cancellation");
            
            Assert.IsFalse(acknowledged);
        }

        [Test]
        public async Task Resume_When_ThereIsNo_ExecutionInfo_Returns_Error()
        {
            // configure repository to not find execution info
            _repositoryInfoMock
                .Setup(x => x.GetAsync<SpecialLiquidationOperationData>(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((IOperationExecutionInfo<SpecialLiquidationOperationData>)null);

            var pauseService = GetSut();

            var errorCode = await pauseService.ResumeAsync("whatever", PauseCancellationSource.Manual, "whatever");
            
            Assert.AreEqual(RfqResumeErrorCode.NotFound, errorCode);
        }

        [Test]
        public async Task Resume_When_ThereIsNo_Active_Pause_Returns_Error()
        {
            var executionInfo = GetExecutionInfoWithState();
            
            // configure repository to find execution info
            _repositoryInfoMock
                .Setup(x => x.GetAsync<SpecialLiquidationOperationData>(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(executionInfo);
            
            // configure repository to not find active pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    It.IsAny<string>(),
                    SpecialLiquidationSaga.Name,
                    RfqPauseService.ActivePredicate))
                .ReturnsAsync(Enumerable.Empty<Pause>());

            var pauseService = GetSut();

            var errorCode = await pauseService.ResumeAsync("whatever", PauseCancellationSource.Manual, "whatever");
            
            Assert.AreEqual(RfqResumeErrorCode.NotPaused, errorCode);
        }

        [Test]
        public async Task Resume_Manually_When_Paused_Not_Manually_Returns_Error()
        {
            var executionInfo = GetExecutionInfoWithState();
            
            // configure repository to find execution info
            _repositoryInfoMock
                .Setup(x => x.GetAsync<SpecialLiquidationOperationData>(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(executionInfo);
            
            // configure repository to return active pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    "active",
                    SpecialLiquidationSaga.Name,
                    RfqPauseService.ActivePredicate))
                .ReturnsAsync(new[] { GetPersistedPause(PauseState.Active, PauseSource.TradingDisabled) });
            
            var pauseService = GetSut();
            
            var errorCode = await pauseService.ResumeAsync("active", PauseCancellationSource.Manual, "whatever");
            
            Assert.AreEqual(RfqResumeErrorCode.ManualResumeDenied, errorCode);
        }

        [Test]
        public async Task Resume_When_ActivePauseExists_Updates_It_And_Returns_Success()
        {
            var executionInfo = GetExecutionInfoWithState();
            
            // configure repository to find execution info
            _repositoryInfoMock
                .Setup(x => x.GetAsync<SpecialLiquidationOperationData>(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(executionInfo);
            
            // configure repository to return active pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    "active",
                    SpecialLiquidationSaga.Name,
                    RfqPauseService.ActivePredicate))
                .ReturnsAsync(new[] { GetPersistedPause(PauseState.Active) });

            // configure repository to succeed when update is called
            _repositoryPauseMock
                .Setup(x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<PauseState>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<Initiator>(),
                    It.IsAny<PauseCancellationSource?>()))
                .ReturnsAsync(true);
            
            var pauseService = GetSut();

            var errorCode = await pauseService.ResumeAsync("active", PauseCancellationSource.Manual, "whatever");

            _repositoryPauseMock.Verify(x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<DateTime?>(),
                    PauseState.PendingCancellation,
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<Initiator>(),
                    It.IsAny<PauseCancellationSource?>()),
                Times.Once);
            
            Assert.AreEqual(RfqResumeErrorCode.None, errorCode);
        }

        [Test]
        public void CalculatePauseSummary_Can_Be_Paused_When_ThereIsNo_Pause_Yet_And_State_Allows()
        {
            var pauseSummary = IRfqPauseService.CalculatePauseSummary(
                new OperationExecutionInfoWithPause<SpecialLiquidationOperationData>(
                    "name",
                    "id",
                    DateTime.UtcNow,
                    new SpecialLiquidationOperationData { State = GetRandomOperationState(true) }) { CurrentPause = null});
            
            Assert.IsTrue(pauseSummary.CanBePaused);
        }

        [Test]
        public void CalculatePauseSummary_Cannot_Be_Paused_When_ThereIsNo_Pause_But_State_IsNotAllowed()
        {
            var pauseSummary = IRfqPauseService.CalculatePauseSummary(
                new OperationExecutionInfoWithPause<SpecialLiquidationOperationData>(
                    "name",
                    "id",
                    DateTime.UtcNow,
                    new SpecialLiquidationOperationData { State = GetRandomOperationState(false) }) { CurrentPause = null });
            
            Assert.IsFalse(pauseSummary.CanBePaused);
        }

        [Test]
        public void CalculatePauseSummary_Cannot_Be_Paused_When_ThereIs_Pause_Already()
        {
            var pauseSummary = IRfqPauseService.CalculatePauseSummary(
                new OperationExecutionInfoWithPause<SpecialLiquidationOperationData>(
                    "name",
                    "id",
                    DateTime.UtcNow,
                    new SpecialLiquidationOperationData
                    {
                        State = GetRandomOperationState()
                    }) { CurrentPause = new OperationExecutionPause { State = PauseState.Active } });
            
            Assert.IsFalse(pauseSummary.CanBePaused);
        }

        [Test]
        public void CalculatePauseSummary_Can_Be_Paused_When_ThereIs_CancelledPause()
        {
            var pauseSummary = IRfqPauseService.CalculatePauseSummary(
                new OperationExecutionInfoWithPause<SpecialLiquidationOperationData>(
                    "name",
                    "id",
                    DateTime.UtcNow,
                    new SpecialLiquidationOperationData
                    {
                        State = GetRandomOperationState(true)
                    }) { CurrentPause = null, LatestCancelledPause = new OperationExecutionPause { State = PauseState.Cancelled } });
            
            Assert.IsTrue(pauseSummary.CanBePaused);
        }

        [Test]
        [TestCaseSource(nameof(PauseStatesCannotCancelIn))]
        public void CalculatePauseSummary_Cannot_Be_Cancelled_When_ThereIsNo_Active_Pause(PauseState pauseState)
        {
            var pauseSummary = IRfqPauseService.CalculatePauseSummary(
                new OperationExecutionInfoWithPause<SpecialLiquidationOperationData>(
                    "name",
                    "id",
                    DateTime.UtcNow,
                    new SpecialLiquidationOperationData
                    {
                        State = GetRandomOperationState()
                    }) { CurrentPause = null});
            
            Assert.IsFalse(pauseSummary.CanBeResumed);
        }

        [Test]
        [TestCase(PauseState.Active)]
        [TestCase(PauseState.PendingCancellation)]
        public void CalculatePauseSummary_Considered_As_Paused_When_Active_Or_Pending_Cancellation(PauseState pauseState)
        {
            var pauseSummary = IRfqPauseService.CalculatePauseSummary(
                new OperationExecutionInfoWithPause<SpecialLiquidationOperationData>(
                    "name",
                    "id",
                    DateTime.UtcNow,
                    new SpecialLiquidationOperationData
                    {
                        State = GetRandomOperationState()
                    }) { CurrentPause = new OperationExecutionPause { State = pauseState } });
            
            Assert.IsTrue(pauseSummary.IsPaused);
        }

        #region Helper methods

        private RfqPauseService GetSut() => new RfqPauseService(
            _repositoryPauseMock.Object,
            _repositoryInfoMock.Object,
            _logMock.Object,
            _dateServiceMock.Object,
            _cqrsSenderMock.Object);

        private static Pause GetPause(PauseState? state = null, PauseSource? source = PauseSource.Manual) => Pause.Create("Id",
            "Name",
            source ?? PauseSource.Manual,
            "initiator",
            DateTime.UtcNow,
            state: state ?? PauseState.Pending, 
            cancellationInitiator: "cancellationInitiator");

        private static Pause GetPersistedPause(PauseState? state = null, PauseSource? source = PauseSource.Manual) => Pause.Initialize(1,
            "id",
            "Name",
            DateTime.UtcNow,
            DateTime.UtcNow, 
            state ?? PauseState.Pending,
            source ?? PauseSource.Manual,
            "initiator",
            null,
            null,
            "cancellationInitiator",
            null);

        private static IOperationExecutionInfo<SpecialLiquidationOperationData> GetExecutionInfoWithState(SpecialLiquidationOperationState? state = null)
        {
            var result = new OperationExecutionInfo<SpecialLiquidationOperationData>(
                SpecialLiquidationSaga.Name,
                "id",
                DateTime.UtcNow,
                new SpecialLiquidationOperationData());
            
            result.Data.State = state ?? default;

            return result;
        }

        private static T GetRandomEnumValue<T>() where T: Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(new Random().Next(values.Length));
        }

        private static SpecialLiquidationOperationState GetRandomOperationState(bool? shouldBeAllowedToPauseIn = null)
        {
            var randomState = GetRandomEnumValue<SpecialLiquidationOperationState>();

            // any state
            if (!shouldBeAllowedToPauseIn.HasValue)
                return randomState;

            // only state which is allowed to pause in
            if (shouldBeAllowedToPauseIn.Value)
            {
                while (!RfqPauseService.AllowedOperationStatesToPauseIn.Contains(randomState))
                {
                    randomState = GetRandomEnumValue<SpecialLiquidationOperationState>();
                }

                return randomState;
            }

            // only state which is not allowed to pause in
            while (RfqPauseService.AllowedOperationStatesToPauseIn.Contains(randomState))
            {
                randomState = GetRandomEnumValue<SpecialLiquidationOperationState>();
            }

            return randomState;
        }

        #endregion
    }
}