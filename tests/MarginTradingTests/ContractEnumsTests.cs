// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.Backend.Contracts.Workflow.Liquidation;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using NUnit.Framework;

namespace MarginTradingTests
{
    public class ContractEnumsTests
    {
        [TestCase(typeof(OrderRejectReason), typeof(OrderRejectReasonContract))]
        [TestCase(typeof(PositionCloseReason), typeof(PositionCloseReasonContract))]
        [TestCase(typeof(LiquidationType), typeof(LiquidationTypeContract))]
        [TestCase(typeof(OriginatorType), typeof(OriginatorTypeContract))]
        public void contract_enum_should_have_same_values_as_domain_enum_has(Type sourceEnumType, Type contractEnumType)
        {
            var sourceEnumValues = Enum.GetValues(sourceEnumType);
            var contractEnumValues = Enum.GetValues(contractEnumType);
            
            Assert.AreEqual(sourceEnumValues.Length, contractEnumValues.Length, "Contract [{0}] and domain [{1}] enums have different length", contractEnumType.Name, sourceEnumType.Name);
            
            foreach (var sourceEnumValue in sourceEnumValues)
            {
                var sourceEnumName = sourceEnumValue.ToString();
                var contractEnumName = Enum.GetName(contractEnumType, sourceEnumValue);
                
                Assert.AreEqual(sourceEnumName, contractEnumName, "Contract [{0}] and domain [{1}] enums have different values", contractEnumType.Name, sourceEnumType.Name);
            }
        }   
    }
}