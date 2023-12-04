// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Workflow;
using Moq;
using NUnit.Framework;
using ExecutionInfo = MarginTrading.Backend.Core.IOperationExecutionInfo<MarginTrading.Backend.Core.SpecialLiquidationOperationData>;

namespace MarginTradingTests.WorkflowTests
{
    [TestFixture]
    public class SpecialLiquidationFailedEventHandlerTests
    {
        [Test]
        public async Task DetermineNextAction_ReturnsComplete_WhenInstrumentIsDiscontinued()
        {
            var result = await SpecialLiquidationFailedEventHandler.DetermineNextAction(null, 
                true, 
                _ => false,
                false, 
                _ => Task.FromResult(false), 
                new SpecialLiquidationSettings());
            
            Assert.AreEqual(SpecialLiquidationFailedEventHandler.NextAction.Complete, result);
        }

        [Test]
        public async Task DetermineNextAction_ReturnsCancel_WhenLiquidityIsEnough()
        {
            var executionInfo = Mock.Of<ExecutionInfo>(x =>
                x.Data == new SpecialLiquidationOperationData { RequestedFromCorporateActions = false });
            
            var result = await SpecialLiquidationFailedEventHandler.DetermineNextAction(executionInfo, 
                false, 
                _ => true,
                false, 
                _ => Task.FromResult(false), 
                new SpecialLiquidationSettings());

            Assert.AreEqual(SpecialLiquidationFailedEventHandler.NextAction.Cancel, result);
        }
        
        [Test]
        public void DetermineNextAction_ChecksLiquidity_IfOnly_NotInitiatedByCorporateActions()
        {
            var executionInfo = Mock.Of<ExecutionInfo>(x =>
                x.Data == new SpecialLiquidationOperationData { RequestedFromCorporateActions = false });
            
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await SpecialLiquidationFailedEventHandler.DetermineNextAction(executionInfo, 
                    false, 
                    _ => throw new InvalidOperationException("Liquidity check expected"),
                    false, 
                    _ => Task.FromResult(false), 
                    new SpecialLiquidationSettings()));
            
            Assert.NotNull(ex);
            Assert.AreEqual("Liquidity check expected", ex.Message);
        }

        [Test]
        public async Task DetermineNextAction_ReturnsRetryPriceRequest_WhenRetryIsRequiredAndCanRetryPriceRequest()
        {
            var executionInfo = Mock.Of<ExecutionInfo>(x =>
                x.Data == new SpecialLiquidationOperationData { RequestedFromCorporateActions = true }); // to skip liquidity check

            var result = await SpecialLiquidationFailedEventHandler.DetermineNextAction(executionInfo,
                false,
                _ => false,
                true,
                _ => Task.FromResult(false),
                new SpecialLiquidationSettings
                    { PriceRequestRetryTimeout = TimeSpan.Zero, RetryPriceRequestForCorporateActions = true });

            Assert.AreEqual(SpecialLiquidationFailedEventHandler.NextAction.RetryPriceRequest, result);
        }
        
        [Test]
        public async Task DetermineNextAction_ChecksForPause_BeforeRetryingPriceRequest()
        {
            var executionInfo = Mock.Of<ExecutionInfo>(x =>
                x.Data == new SpecialLiquidationOperationData { RequestedFromCorporateActions = true }); // to skip liquidity check

            var result = await SpecialLiquidationFailedEventHandler.DetermineNextAction(executionInfo,
                false,
                _ => false,
                true,
                _ => Task.FromResult(true),
                new SpecialLiquidationSettings
                    { PriceRequestRetryTimeout = TimeSpan.Zero, RetryPriceRequestForCorporateActions = true });

            Assert.AreEqual(SpecialLiquidationFailedEventHandler.NextAction.Pause, result);
        }
        
        [Test]
        public async Task DetermineNextAction_ResumesInitialFlow_WhenHasCausingLiquidation()
        {
            var executionInfo = Mock.Of<ExecutionInfo>(x =>
                x.Data == new SpecialLiquidationOperationData
                {
                    CausationOperationId = "123", 
                    RequestedFromCorporateActions = true
                });

            var result = await SpecialLiquidationFailedEventHandler.DetermineNextAction(executionInfo,
                false,
                _ => false,
                false,
                _ => Task.FromResult(false),
                new SpecialLiquidationSettings
                    { PriceRequestRetryTimeout = TimeSpan.Zero, RetryPriceRequestForCorporateActions = true });

            Assert.AreEqual(SpecialLiquidationFailedEventHandler.NextAction.ResumeInitialFlow, result);
        }
        
        [Test]
        public async Task DetermineNextAction_ReturnsComplete_WhenAsDefault()
        {
            var executionInfo = Mock.Of<ExecutionInfo>(x =>
                x.Data == new SpecialLiquidationOperationData
                {
                    CausationOperationId = null,
                    RequestedFromCorporateActions = false
                });

            var result = await SpecialLiquidationFailedEventHandler.DetermineNextAction(executionInfo,
                false,
                _ => false,
                false,
                _ => Task.FromResult(false),
                new SpecialLiquidationSettings { PriceRequestRetryTimeout = null, });
            
            Assert.AreEqual(SpecialLiquidationFailedEventHandler.NextAction.Complete, result);
        }
    }
}