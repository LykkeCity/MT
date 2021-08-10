using MarginTrading.Backend.Contracts.Rfq;
using MarginTrading.Backend.Core;
using NUnit.Framework;
using System;

namespace MarginTradingTests
{
    public class RfqControllerTests
    {
        [Test]
        public void All_rfq_operation_states_have_corresponding_special_liquidation_states_and_vice_versa()
        {
            var rfqStates = Enum.GetNames(typeof(RfqOperationState));
            var specialLiquidationStates = Enum.GetNames(typeof(SpecialLiquidationOperationState));

            CollectionAssert.AreEquivalent(rfqStates, specialLiquidationStates);
        }
    }
}