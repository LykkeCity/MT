// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTradingTests.Helpers;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class OrderFulfillmentPlanTests
    {
        [Test]
        public void Create_Positions_Of_Different_Asset_Raises_Exception()
        {
            Assert.Throws<OrderFulfillmentPlanException>(() =>
                OrderFulfillmentPlan.Create(
                    DumbDataGenerator.GenerateOrder(assetPairId: "1"),
                    DumbDataGenerator.GeneratePosition(assetPairId: "2")));
        }

        [Test]
        public void Create_With_Inactive_Positions_Raises_Exception()
        {
            var p1 = DumbDataGenerator.GeneratePosition();
            p1.StartClosing(DateTime.UtcNow, PositionCloseReason.None, OriginatorType.System, string.Empty);

            var p2 = DumbDataGenerator.GeneratePosition();

            Assert.Throws<OrderFulfillmentPlanException>(() =>
                OrderFulfillmentPlan.Create(
                    DumbDataGenerator.GenerateOrder(),
                    new List<Position> { p1, p2 }));
        }

        [Test]
        public void Create_With_Positions_Of_Same_As_Order_Direction_Raises_Exception()
        {
            Assert.Throws<OrderFulfillmentPlanException>(() =>
                OrderFulfillmentPlan.Create(
                    DumbDataGenerator.GenerateOrder(volume: 100),
                    DumbDataGenerator.GeneratePosition(volume: 10)));
        }
        
        [Test]
        public void Create_With_Positions_Of_Different_Account_Raises_Exception()
        {
            Assert.Throws<OrderFulfillmentPlanException>(() =>
                OrderFulfillmentPlan.Create(
                    DumbDataGenerator.GenerateOrder(volume: 100, accountId: "acc1"),
                    DumbDataGenerator.GeneratePosition(volume: -50, accountId: "acc2")));
        }

        [Test]
        public void Create_With_Empty_Positions_Success()
        {
            Assert.DoesNotThrow(() => 
                OrderFulfillmentPlan.Create(
                    DumbDataGenerator.GenerateOrder(), 
                Enumerable.Empty<Position>().ToList()));
        }

        [TestCase(100, -105, false)]
        [TestCase(100, -95, true)]
        [TestCase(100, -100, false)]
        [TestCase(-100, 105, false)]
        [TestCase(-100, 95, true)]
        [TestCase(-100, 100, false)]
        public void Create_(decimal orderVolume, decimal totalPositionsVolume, bool requiresPositionOpening)
        {
            var fulfillmentPlan = OrderFulfillmentPlan.Create(
                DumbDataGenerator.GenerateOrder(volume: orderVolume),
                DumbDataGenerator.GeneratePosition(volume: totalPositionsVolume));
            
            Assert.AreEqual(requiresPositionOpening, fulfillmentPlan.RequiresPositionOpening);
        }
    }
}