// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Builders;
using MarginTradingTests.Helpers;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class DealBuilderTests : BaseTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RegisterDependencies();
        }

        [Test]
        public void Order_Executed_Is_Required_When_Closing_Position()
        {
            var builder = new DealBuilder(new Position(), CreateOrder(1));

            Assert.Throws(
                Is.TypeOf<InvalidOperationException>().And.Message
                    .EqualTo("Order.Executed must be set to create deal from"), () => DealDirector.Construct(builder));
        }
        
        [Test]
        public void Order_Executed_Is_Required_When_Partially_Closing_Position()
        {
            var builder = new PartialDealBuilder(new Position(), CreateOrder(1), 0);

            Assert.Throws(
                Is.TypeOf<InvalidOperationException>().And.Message
                    .EqualTo("Order.Executed must be set to create deal from"), () => DealDirector.Construct(builder));
        }

        [Test]
        [TestCase(10, 1.2, 100, 1.3, 1.0)]
        [TestCase(10, 1.2, 100, 1.1, -1.0)]
        [TestCase(-10, 1.2, 100, 1.1, 1.0)]
        [TestCase(-10, 1.2, 100, 1.3, -1.0)]
        public void Pnl_Is_Correct_When_Fully_Closing_Position(decimal openedPositionVolume,
            decimal openedPositionPrice,
            decimal matchedOrderVolume,
            decimal matchedOrderPrice,
            decimal expectedPnl)
        {
            // The case: we have an opened position which is supposed to be FULLY closed by a new order.
            
            Assert.Less(Math.Abs(openedPositionVolume), Math.Abs(matchedOrderVolume));

            var openedPosition = CreateOpenedPosition(openedPositionVolume, openedPositionPrice);
            var closeOrder = CreateOrder(openedPositionVolume);

            ExecutePnlFlow(openedPosition, closeOrder, matchedOrderVolume, matchedOrderPrice);
            var builder = new DealBuilder(openedPosition, closeOrder);
            var deal = DealDirector.Construct(builder);

            Assert.AreEqual(expectedPnl, deal.Fpl);
            Assert.AreEqual(openedPosition.Id, deal.DealId);
        }
        
        [Test]
        [TestCase(10, 1.2, 100, 1.3, 0.5, 0.5)]
        [TestCase(10, 1.2, 100, 1.3, 1.0, 0)]
        [TestCase(10, 1.2, 100, 1.3, 1.5, -0.5)]
        [TestCase(10, 1.2, 100, 1.1, 1.0, -2.0)]
        [TestCase(-10, 1.2, 100, 1.1, 0.5, 0.5)]
        [TestCase(-10, 1.2, 100, 1.1, 1, 0)]
        [TestCase(-10, 1.2, 100, 1.1, 1.5, -0.5)]
        [TestCase(-10, 1.2, 100, 1.3, 1.0, -2.0)]
        public void PnlOfTheDay_Is_Correct_When_Fully_Closing_Position(decimal openedPositionVolume,
            decimal openedPositionPrice,
            decimal matchedOrderVolume,
            decimal matchedOrderPrice,
            decimal positionChargedPnl,
            decimal expectedPnlOfTheDay)
        {
            // The case: we have an opened position which is supposed to be FULLY closed by a new order.
            
            Assert.Less(Math.Abs(openedPositionVolume), Math.Abs(matchedOrderVolume));
            
            var openedPosition = CreateOpenedPosition(openedPositionVolume, openedPositionPrice);
            var closeOrder = CreateOrder(openedPositionVolume);
            
            ExecutePnlOfTheDayFlow(openedPosition, closeOrder, matchedOrderVolume, matchedOrderPrice, positionChargedPnl);
            var builder = new DealBuilder(openedPosition, closeOrder);
            var deal = DealDirector.Construct(builder);
            
            Assert.AreEqual(expectedPnlOfTheDay, deal.PnlOfTheLastDay);
        }

        [Test]
        [TestCase(10, 1.2, 5, 100, 1.3, 0.5)]
        [TestCase(10, 1.2, 5, 100, 1.1, -0.5)]
        [TestCase(-10, 1.2, 5, 100, 1.1, 0.5)]
        [TestCase(-10, 1.2, 5, 100, 1.3, -0.5)]
        public void Pnl_Is_Correct_When_Partially_Closing_Position(decimal openedPositionVolume,
            decimal openedPositionPrice,
            decimal closePositionVolume,
            decimal matchedOrderVolume,
            decimal matchedOrderPrice,
            decimal expectedPnl)
        {
            // The case: we have an opened position which is supposed to be PARTIALLY closed by a new order.
            
            Assert.Less(Math.Abs(closePositionVolume), Math.Abs(openedPositionVolume));
            Assert.Less(Math.Abs(closePositionVolume), Math.Abs(matchedOrderVolume));
            
            var openedPosition = CreateOpenedPosition(openedPositionVolume, openedPositionPrice);
            var closeOrder = CreateOrder(closePositionVolume);
            
            ExecutePnlFlowPartiallyClose(openedPosition, closeOrder, closePositionVolume,matchedOrderVolume, matchedOrderPrice);
            var builder = new PartialDealBuilder(openedPosition, closeOrder, closeOrder.Volume);
            var deal = DealDirector.Construct(builder);
            
            Assert.AreEqual(expectedPnl, deal.Fpl);
            Assert.AreNotEqual(openedPosition.Id, deal.DealId);
        }

        [Test]
        [TestCase(10, 1.2, 5, 100, 1.3, 0.5, 0.5)]
        [TestCase(10, 1.2, 5, 100, 1.3, 1.0, 0.5)]
        [TestCase(10, 1.2, 5, 100, 1.3, 1.5, 0.5)]
        [TestCase(10, 1.2, 5, 100, 1.1, 1.0, -0.5)]
        [TestCase(-10, 1.2, 5, 100, 1.1, 0.5, 0.5)]
        [TestCase(-10, 1.2, 5, 100, 1.1, 1, 0.5)]
        [TestCase(-10, 1.2, 5, 100, 1.1, 1.5, 0.5)]
        [TestCase(-10, 1.2, 5, 100, 1.3, 1.0, -0.5)]
        public void PnlOfTheDay_Is_Correct_When_Partially_Closing_Position(decimal openedPositionVolume,
            decimal openedPositionPrice,
            decimal closePositionVolume,
            decimal matchedOrderVolume,
            decimal matchedOrderPrice,
            decimal positionChargedPnl,
            decimal expectedPnlOfTheDay)
        {
            // The case: we have an opened position which is supposed to be PARTIALLY closed by a new order.
            
            Assert.Less(Math.Abs(closePositionVolume), Math.Abs(openedPositionVolume));
            Assert.Less(Math.Abs(closePositionVolume), Math.Abs(matchedOrderVolume));
            
            var openedPosition = CreateOpenedPosition(openedPositionVolume, openedPositionPrice);
            var closeOrder = CreateOrder(closePositionVolume);

            ExecutePnlOfTheDayFlowPartiallyClose(openedPosition,
                closeOrder,
                closePositionVolume,
                matchedOrderVolume,
                matchedOrderPrice,
                positionChargedPnl);
            var builder = new PartialDealBuilder(openedPosition, closeOrder, closeOrder.Volume);
            var deal = DealDirector.Construct(builder);
            
            Assert.AreEqual(expectedPnlOfTheDay, deal.PnlOfTheLastDay);
        }

        [Test]
        [Repeat(100)]
        [TestCase(100, 1.2, 200, 500, 1.3, 0.5)]
        public void Deal_Volume_Cannot_Be_Greater_Than_Position_Volume_When_Partially_Closing_Position(decimal initialPositionVolume, 
            decimal openedPositionPrice,
            decimal closeOrderVolume,
            decimal matchedOrderVolume,
            decimal matchedOrderPrice,
            decimal positionChargedPnl)
        {
            var openedPosition = CreateOpenedPosition(initialPositionVolume, openedPositionPrice);
            // assuming, there were other positions closed as a part of order
            // execution so that left volume to match will be less than original
            // position volume
            var closeOrder = CreateOrder(closeOrderVolume);
            
            Assert.True(closeOrderVolume > initialPositionVolume,
                "The point of test is to check the case when order spans multiple positions therefore it volume is greater than single position volume");

            ExecutePnlOfTheDayFlowPartiallyClose(openedPosition,
                closeOrder,
                closeOrder.Volume,
                matchedOrderVolume,
                matchedOrderPrice,
                positionChargedPnl);

            var partiallyClosingVolume = new Random().Next(1, 100) * initialPositionVolume / 100;
            var builder = new PartialDealBuilder(openedPosition, closeOrder, partiallyClosingVolume);
            var deal = DealDirector.Construct(builder);

            var expectedDealVolume = Math.Abs(deal.Volume);
            var expectedPositionVolume = Math.Abs(openedPosition.Volume);

            Assert.True(expectedDealVolume <= expectedPositionVolume,
                "Deal volume [{0}] cannot be greater than position it partially closes [{1}]", expectedDealVolume, expectedPositionVolume);
        }
        
        private Position CreateOpenedPosition(decimal volume, decimal price)
        {
            return TestObjectsFactory.CreateOpenedPosition("EURUSD", 
                Accounts[0], 
                MarginTradingTestsUtils.TradingConditionId, 
                volume, 
                price);
        }

        private Order CreateOrder(decimal volume)
        {
            return TestObjectsFactory.CreateNewOrder(OrderType.Market, 
                "EURUSD", 
                Accounts[0], 
                MarginTradingTestsUtils.TradingConditionId, 
                volume);
        }
        
        # region Flows

        private void ExecutePnlFlow(Position position, Order order, decimal matchedVolume, decimal matchedPrice)
        {
            order.StartExecution(DateTime.UtcNow, "fakeMatchingEngineId");
            
            order.Execute(DateTime.UtcNow, new MatchedOrderCollection(new List<MatchedOrder>
            {
                new MatchedOrder { IsExternal = false, Volume = matchedVolume, Price = matchedPrice }
            }), AssetsConstants.DefaultAssetAccuracy);
            
            position.StartClosing(DateTime.UtcNow, PositionCloseReason.None, OriginatorType.System, string.Empty);
            
            position.Close(DateTime.UtcNow, "fakeMatchingEngineId", matchedPrice, 0, 0,
                OriginatorType.System, PositionCloseReason.None, string.Empty, "CloseTrade");
        }

        private void ExecutePnlOfTheDayFlow(Position position, Order order, decimal matchedVolume, decimal matchedPrice, decimal positionChargedPnl)
        {
            order.StartExecution(DateTime.UtcNow, "fakeMatchingEngineId");
            
            order.Execute(DateTime.UtcNow, new MatchedOrderCollection(new List<MatchedOrder>
            {
                new MatchedOrder { IsExternal = false, Volume = matchedVolume, Price = matchedPrice }
            }), AssetsConstants.DefaultAssetAccuracy);
            
            position.SetChargedPnL("any-operation-id", positionChargedPnl);
            
            position.StartClosing(DateTime.UtcNow, PositionCloseReason.None, OriginatorType.System, string.Empty);
            
            position.Close(DateTime.UtcNow, "fakeMatchingEngineId", matchedPrice, 0, 0,
                OriginatorType.System, PositionCloseReason.None, string.Empty, "CloseTrade");
        }

        private void ExecutePnlFlowPartiallyClose(Position position,
            Order order,
            decimal closeVolume,
            decimal matchedVolume,
            decimal matchedPrice)
        {
            order.StartExecution(DateTime.UtcNow, "fakeMatchingEngineId");
            
            order.Execute(DateTime.UtcNow, new MatchedOrderCollection(new List<MatchedOrder>
            {
                new MatchedOrder { IsExternal = false, Volume = matchedVolume, Price = matchedPrice }
            }), AssetsConstants.DefaultAssetAccuracy);
            
            position.StartClosing(DateTime.UtcNow, PositionCloseReason.None, OriginatorType.System, string.Empty);
            
            position.PartiallyClose(DateTime.UtcNow, closeVolume, "CloseTrade", 0);
        }
        
        private void ExecutePnlOfTheDayFlowPartiallyClose(Position position,
            Order order,
            decimal closeVolume,
            decimal matchedVolume,
            decimal matchedPrice,
            decimal positionChargedPnl)
        {
            order.StartExecution(DateTime.UtcNow, "fakeMatchingEngineId");
            
            order.Execute(DateTime.UtcNow, new MatchedOrderCollection(new List<MatchedOrder>
            {
                new MatchedOrder { IsExternal = false, Volume = matchedVolume, Price = matchedPrice }
            }), AssetsConstants.DefaultAssetAccuracy);
            
            position.SetChargedPnL("any-operation-id", positionChargedPnl);
            
            position.StartClosing(DateTime.UtcNow, PositionCloseReason.None, OriginatorType.System, string.Empty);
            
            position.PartiallyClose(DateTime.UtcNow, closeVolume, "CloseTrade", positionChargedPnl);
        }
        
        #endregion
    }
}