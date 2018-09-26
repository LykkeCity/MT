using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SagaExtensionsTests
    {
        [Test]
        public void TestSwitchState()
        {
            WithdrawalFreezeOperationData data = null;

            Assert.Throws<InvalidOperationException>(() =>
                data.SwitchState(OperationState.Initiated, OperationState.Started));
            
            data = new WithdrawalFreezeOperationData {State = OperationState.Initiated};

            Assert.Throws<InvalidOperationException>(() =>
                data.SwitchState(OperationState.Started, OperationState.Finished));
            
            Assert.IsTrue(data.SwitchState(OperationState.Initiated, OperationState.Started));
            
            Assert.IsFalse(data.SwitchState(OperationState.Initiated, OperationState.Started));
            
            Assert.IsTrue(data.SwitchState(OperationState.Started, OperationState.Finished));

            Assert.AreEqual(OperationState.Finished, data.State);
        }
        
    }
}