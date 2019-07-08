// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;
using Common;
using MarginTrading.Backend.Core.MatchedOrders;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class MatchedOrderCollectionTests
    {
        [Test]
        public void Test_Serialization()
        {
            var list = new List<MatchedOrder> {new MatchedOrder()};
            var collection = new MatchedOrderCollection(list);

            var listJson = list.ToJson();
            var collectionJson = collection.ToJson();

            var newList = collectionJson.DeserializeJson<List<MatchedOrder>>();
            var newCollection = listJson.DeserializeJson<MatchedOrderCollection>();

            Assert.AreEqual(list.Count, newList.Count);
            Assert.AreEqual(collection.Count, newCollection.Count);
        }
    }
}
