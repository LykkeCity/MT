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
        public void Create_Sentiment_With_Negative_Counter_Results_In_Zero_Share()
        {
            var sentiment = new Sentiment("test product", -1);

            var (shortShare, longShare) = sentiment;
            
            Assert.AreEqual(0, shortShare);
            Assert.AreEqual(0, longShare);
        }
        
        [Test]
        public void Sentiment_Long_Share_Is_The_Remainder_Based_On_Short_Share()
        {
            var sentiment = new Sentiment("test product", 8, 1);
            
            var (shortShare, longShare) = sentiment;
            
            Assert.AreEqual(88.89, shortShare);
            Assert.AreEqual(100 - 88.89, longShare);
        }
        
        [Test]
        public void Sentiment_Remove_Counter_Less_Than_Zero_Results_In_Zero_Share()
        {
            var sentiment = new Sentiment("test product", 8, 1);
            
            sentiment = sentiment.RemoveLong(2);
            
            var (shortShare, longShare) = sentiment;
            
            Assert.AreEqual(100, shortShare);
            Assert.AreEqual(0, longShare);
        }
    }
}