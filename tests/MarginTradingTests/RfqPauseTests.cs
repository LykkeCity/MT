// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Contracts.Common;
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
            var executionInfo = GetExecutionInfoWithState(GetRandomNotAllowedOperationState());
            
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
                    SpecialLiquidationSaga.OperationName,
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
                    SpecialLiquidationSaga.OperationName,
                    RfqPauseService.ActivePredicate))
                .ReturnsAsync(Enumerable.Empty<Pause>());

            
            // configure repository to return pending pause
            _repositoryPauseMock
                .Setup(x => x.FindAsync(
                    "pending",
                    SpecialLiquidationSaga.OperationName,
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
                    It.IsAny<Initiator?>(),
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
                    SpecialLiquidationSaga.OperationName,
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
                    SpecialLiquidationSaga.OperationName,
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
                    It.IsAny<Initiator?>(),
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
                    SpecialLiquidationSaga.OperationName,
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
                    It.IsAny<Initiator?>(),
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
                    It.IsAny<Initiator?>(),
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
                    SpecialLiquidationSaga.OperationName,
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
                    SpecialLiquidationSaga.OperationName,
                    RfqPauseService.ActivePredicate))
                .ReturnsAsync(Enumerable.Empty<Pause>());

            var pauseService = GetSut();

            var errorCode = await pauseService.ResumeAsync("whatever", PauseCancellationSource.Manual, "whatever");
            
            Assert.AreEqual(RfqResumeErrorCode.NotPaused, errorCode);
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
                    SpecialLiquidationSaga.OperationName,
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
                    It.IsAny<Initiator?>(),
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
                    It.IsAny<Initiator?>(),
                    It.IsAny<PauseCancellationSource?>()),
                Times.Once);
            
            Assert.AreEqual(RfqResumeErrorCode.None, errorCode);
        }

        #region Helper methods

        private RfqPauseService GetSut() => new RfqPauseService(
            _repositoryPauseMock.Object,
            _repositoryInfoMock.Object,
            _logMock.Object,
            _dateServiceMock.Object,
            _cqrsSenderMock.Object);

        private static Pause GetPause(PauseState? state = null) => Pause.Create("Id",
            "Name",
            DateTime.UtcNow,
            DateTime.UtcNow, 
            state ?? PauseState.Pending,
            PauseSource.Manual,
            "initiator",
            null,
            null,
            "cancellationInitiator",
            null);

        private static Pause GetPersistedPause(PauseState? state = null) => Pause.Initialize(1,
            "id",
            "Name",
            DateTime.UtcNow,
            DateTime.UtcNow, 
            state ?? PauseState.Pending,
            PauseSource.Manual,
            "initiator",
            null,
            null,
            "cancellationInitiator",
            null);

        private static IOperationExecutionInfo<SpecialLiquidationOperationData> GetExecutionInfoWithState(SpecialLiquidationOperationState? state = null)
        {
            var result = new OperationExecutionInfo<SpecialLiquidationOperationData>(
                SpecialLiquidationSaga.OperationName,
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

        private static SpecialLiquidationOperationState GetRandomNotAllowedOperationState()
        {
            SpecialLiquidationOperationState? randomState;
            
            do
            {
                randomState = GetRandomEnumValue<SpecialLiquidationOperationState>();
                
            } while (RfqPauseService.AllowedOperationStatesToPauseIn.Contains(randomState.Value));

            return randomState.Value;
        }
        
        #endregion
    }
}