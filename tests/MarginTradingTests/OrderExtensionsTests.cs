// Copyright (c) 2019 Lykke Corp.

using System;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTradingTests.Helpers;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class OrderExtensionsTests
    {
        #region GetSortedForExecution
        
        //https://docs.google.com/spreadsheets/d/1_hpEl01hvq-w-ATGFt9PCboRG32I8iKrHtyY17nnlTI/edit#gid=1229004254
        
        [Test]
        public void Test_GetSortedForExecution_T01a()
        {
            var zeroTime = DateTime.UtcNow;
 
            var order1 = CreatePendingOrder(OrderType.Stop, OrderDirection.Buy, 104, zeroTime);
            var order2 = CreatePendingOrder(OrderType.Stop, OrderDirection.Buy, 105, zeroTime.AddSeconds(-1));
            var order3 = CreatePendingOrder(OrderType.Stop, OrderDirection.Buy, 105, zeroTime);
            var order4 = CreatePendingOrder(OrderType.Limit, OrderDirection.Sell, 103, zeroTime);
            var order5 = CreatePendingOrder(OrderType.Limit, OrderDirection.Sell, 104, zeroTime.AddSeconds(-1));
            var order6 = CreatePendingOrder(OrderType.Limit, OrderDirection.Sell, 104, zeroTime);

            var orders = new[] {order5, order6, order4, order2, order3, order1};
            var sortedOrders = orders.GetSortedForExecution().ToArray();
            
            Assert.AreEqual(sortedOrders[0].Id, order1.Id);
            Assert.AreEqual(sortedOrders[1].Id, order2.Id);
            Assert.AreEqual(sortedOrders[2].Id, order3.Id);
            Assert.AreEqual(sortedOrders[3].Id, order4.Id);
            Assert.AreEqual(sortedOrders[4].Id, order5.Id);
            Assert.AreEqual(sortedOrders[5].Id, order6.Id);
        }
        
        [Test]
        public void Test_GetSortedForExecution_T01b()
        {
            var zeroTime = DateTime.UtcNow;

            var order1 = CreatePendingOrder(OrderType.Stop, OrderDirection.Sell, 99, zeroTime);
            var order2 = CreatePendingOrder(OrderType.Stop, OrderDirection.Sell, 98, zeroTime.AddSeconds(-1));
            var order3 = CreatePendingOrder(OrderType.Stop, OrderDirection.Sell, 98, zeroTime);
            var order4 = CreatePendingOrder(OrderType.Stop, OrderDirection.Buy, 104, zeroTime);
            var order5 = CreatePendingOrder(OrderType.Stop, OrderDirection.Buy, 105, zeroTime.AddSeconds(-1));
            var order6 = CreatePendingOrder(OrderType.Stop, OrderDirection.Buy, 105, zeroTime);

            var orders = new[] {order5, order6, order4, order2, order3, order1};
            var sortedOrders = orders.GetSortedForExecution().ToArray();
            
            Assert.AreEqual(sortedOrders[0].Id, order1.Id);
            Assert.AreEqual(sortedOrders[1].Id, order2.Id);
            Assert.AreEqual(sortedOrders[2].Id, order3.Id);
            Assert.AreEqual(sortedOrders[3].Id, order4.Id);
            Assert.AreEqual(sortedOrders[4].Id, order5.Id);
            Assert.AreEqual(sortedOrders[5].Id, order6.Id);
        }
        
        [Test]
        public void Test_GetSortedForExecution_T01c()
        {
            var zeroTime = DateTime.UtcNow;

            var order1 = CreatePendingOrder(OrderType.Stop, OrderDirection.Sell, 99, zeroTime);
            var order2 = CreatePendingOrder(OrderType.Stop, OrderDirection.Sell, 98, zeroTime.AddSeconds(-1));
            var order3 = CreatePendingOrder(OrderType.Stop, OrderDirection.Sell, 98, zeroTime);
            var order4 = CreatePendingOrder(OrderType.Limit, OrderDirection.Buy, 100, zeroTime);
            var order5 = CreatePendingOrder(OrderType.Limit, OrderDirection.Buy, 99, zeroTime.AddSeconds(-1));
            var order6 = CreatePendingOrder(OrderType.Limit, OrderDirection.Buy, 99, zeroTime);

            var orders = new[] {order5, order6, order4, order2, order3, order1};
            var sortedOrders = orders.GetSortedForExecution().ToArray();
            
            Assert.AreEqual(sortedOrders[0].Id, order1.Id);
            Assert.AreEqual(sortedOrders[1].Id, order2.Id);
            Assert.AreEqual(sortedOrders[2].Id, order3.Id);
            Assert.AreEqual(sortedOrders[3].Id, order4.Id);
            Assert.AreEqual(sortedOrders[4].Id, order5.Id);
            Assert.AreEqual(sortedOrders[5].Id, order6.Id);
        }
        
        [Test]
        public void Test_GetSortedForExecution_T01d()
        {
            var zeroTime = DateTime.UtcNow;

            var order1 = CreatePendingOrder(OrderType.Limit, OrderDirection.Sell, 103, zeroTime);
            var order2 = CreatePendingOrder(OrderType.Limit, OrderDirection.Sell, 104, zeroTime.AddSeconds(-1));
            var order3 = CreatePendingOrder(OrderType.Limit, OrderDirection.Sell, 104, zeroTime);
            var order4 = CreatePendingOrder(OrderType.Limit, OrderDirection.Buy, 100, zeroTime);
            var order5 = CreatePendingOrder(OrderType.Limit, OrderDirection.Buy, 99, zeroTime.AddSeconds(-1));
            var order6 = CreatePendingOrder(OrderType.Limit, OrderDirection.Buy, 99, zeroTime);

            var orders = new[] {order5, order6, order4, order2, order3, order1};
            var sortedOrders = orders.GetSortedForExecution().ToArray();
            
            Assert.AreEqual(sortedOrders[0].Id, order1.Id);
            Assert.AreEqual(sortedOrders[1].Id, order2.Id);
            Assert.AreEqual(sortedOrders[2].Id, order3.Id);
            Assert.AreEqual(sortedOrders[3].Id, order4.Id);
            Assert.AreEqual(sortedOrders[4].Id, order5.Id);
            Assert.AreEqual(sortedOrders[5].Id, order6.Id);
        }

        private Order CreatePendingOrder(OrderType orderType, OrderDirection direction, decimal? price, DateTime created)
        {
            return TestObjectsFactory.CreateNewOrder(orderType, "AssetPair", new MarginTradingAccount(),
                "TradingCondition", direction == OrderDirection.Buy ? 1 : -1, price: price, created: created);
        }
        
        #endregion
    }
}