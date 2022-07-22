// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SentimentTests
    {
        [Test]
        public void Create_Sentiment_With_Negative_Counter_Results_In_Zero_Counter()
        {
            var sentiment = new Sentiment("test product", -1);

            var (shortCounter, longCounter) = sentiment;
            
            Assert.AreEqual(0, shortCounter);
            Assert.AreEqual(0, longCounter);
        }
        
        [Test]
        public void Sentiment_Long_Counter_Is_The_Remainder_Based_On_Short_Counter()
        {
            var sentiment = new Sentiment("test product", 8, 1);
            
            var (shortCounter, longCounter) = sentiment;
            
            Assert.AreEqual(88.89, shortCounter);
            Assert.AreEqual(100 - 88.89, longCounter);
        }
    }
}