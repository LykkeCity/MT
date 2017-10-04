using System.Collections.Generic;
using System.Linq;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Services;
using MarginTrading.Services.Events;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class AggregatedOrderBookTests : BaseTests
    {
        private AggregatedOrderBook _aggregatedOrderbook;
        private Mock<IEventChannel<BestPriceChangeEventArgs>> _bestPriceChannelMock;

        [Test]
        public void Check_If_Aggregated_Oredrbook_Correct()
        {
            RegisterDependencies();
            _aggregatedOrderbook = Container.Resolve<AggregatedOrderBook>();

            var changeEventArgs = new OrderBookChangeEventArgs
            {
                Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>
                {
                    { "BTCUSD", new Dictionary<decimal, OrderBookLevel>
                    {
                        { 100, new OrderBookLevel {Instrument = "BTCUSD", Volume = 10, Price = 100, Direction = OrderDirection.Buy} },
                        { 120, new OrderBookLevel {Instrument = "BTCUSD", Volume = 5, Price = 120, Direction = OrderDirection.Sell} }
                    } }
                }
            };

            _aggregatedOrderbook.ConsumeEvent(this, changeEventArgs);

            var buyList = _aggregatedOrderbook.GetBuy("BTCUSD");
            var sellList = _aggregatedOrderbook.GetSell("BTCUSD");

            Assert.IsTrue(buyList.Any(item => item.Price  == 100));
            Assert.IsTrue(sellList.All(item => item.Price == 120));
        }

        [Test]
        public void Check_Price_Change_In_Orderbook()
        {
            RegisterDependencies();
            _aggregatedOrderbook = Container.Resolve<AggregatedOrderBook>();

            var changeEventArgs = new OrderBookChangeEventArgs
            {
                Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>
                {
                    { "BTCUSD", new Dictionary<decimal, OrderBookLevel>
                    {
                        { 100, new OrderBookLevel {Instrument = "BTCUSD", Volume = 10, Price = 100, Direction = OrderDirection.Buy} },
                        { 120, new OrderBookLevel {Instrument = "BTCUSD", Volume = 5, Price = 120, Direction = OrderDirection.Sell} }
                    } }
                }
            };

            _aggregatedOrderbook.ConsumeEvent(this, changeEventArgs);

            changeEventArgs = new OrderBookChangeEventArgs
            {
                Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>
                {
                    { "BTCUSD", new Dictionary<decimal, OrderBookLevel>
                    {
                        { 100, new OrderBookLevel {Instrument = "BTCUSD", Volume = 0, Price = 100, Direction = OrderDirection.Buy} },
                        { 99, new OrderBookLevel {Instrument = "BTCUSD", Volume = 5, Price = 99, Direction = OrderDirection.Buy} }
                    } }
                }
            };

            _aggregatedOrderbook.ConsumeEvent(this, changeEventArgs);

            changeEventArgs = new OrderBookChangeEventArgs
            {
                Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>
                {
                    { "BTCUSD", new Dictionary<decimal, OrderBookLevel>
                    {
                        { 120, new OrderBookLevel {Instrument = "BTCUSD", Volume = 0, Price = 120, Direction = OrderDirection.Sell} },
                        { 119, new OrderBookLevel {Instrument = "BTCUSD", Volume = 5, Price = 119, Direction = OrderDirection.Sell} }
                    } }
                }
            };

            _aggregatedOrderbook.ConsumeEvent(this, changeEventArgs);


            var buyList = _aggregatedOrderbook.GetBuy("BTCUSD");
            var sellList = _aggregatedOrderbook.GetSell("BTCUSD");

            Assert.IsTrue(buyList.Count == 1);
            Assert.IsTrue(sellList.Count == 1);
            Assert.IsTrue(buyList.First().Price == 99);
            Assert.IsTrue(sellList.First().Price == 119);
        }

        [Test]
        public void Check_BestPrice_Event_Has_Correct_Price()
        {
            RegisterDependencies(true);
            _aggregatedOrderbook = Container.Resolve<AggregatedOrderBook>();
            var bestPriceChannel = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _bestPriceChannelMock = Mock.Get(bestPriceChannel);

            var changeEventArgs = new OrderBookChangeEventArgs
            {
                Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>
                {
                    { "BTCUSD", new Dictionary<decimal, OrderBookLevel>
                    {
                        { 100, new OrderBookLevel {Instrument = "BTCUSD", Volume = 10, Price = 100, Direction = OrderDirection.Buy} },
                        { 120, new OrderBookLevel {Instrument = "BTCUSD", Volume = 5, Price = 120, Direction = OrderDirection.Sell} }
                    } }
                }
            };

            _aggregatedOrderbook.ConsumeEvent(this, changeEventArgs);

            _bestPriceChannelMock.Verify(x => x.SendEvent(It.IsAny<object>(), It.Is<BestPriceChangeEventArgs>(price => price.BidAskPair.Ask == 120 && price.BidAskPair.Bid == 100)), Times.Once);

            changeEventArgs = new OrderBookChangeEventArgs
            {
                Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>
                {
                    { "BTCUSD", new Dictionary<decimal, OrderBookLevel>
                    {
                        { 100, new OrderBookLevel {Instrument = "BTCUSD", Volume = 0, Price = 100, Direction = OrderDirection.Buy} },
                        { 99, new OrderBookLevel {Instrument = "BTCUSD", Volume = 5, Price = 99, Direction = OrderDirection.Buy} }
                    } }
                }
            };

            _aggregatedOrderbook.ConsumeEvent(this, changeEventArgs);

            _bestPriceChannelMock.Verify(x => x.SendEvent(It.IsAny<object>(), It.Is<BestPriceChangeEventArgs>(price => price.BidAskPair.Ask == 120 && price.BidAskPair.Bid == 99)), Times.Once);

            changeEventArgs = new OrderBookChangeEventArgs
            {
                Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>
                {
                    { "BTCUSD", new Dictionary<decimal, OrderBookLevel>
                    {
                        { 120, new OrderBookLevel {Instrument = "BTCUSD", Volume = 0, Price = 120, Direction = OrderDirection.Sell} },
                        { 119, new OrderBookLevel {Instrument = "BTCUSD", Volume = 5, Price = 98, Direction = OrderDirection.Sell} }
                    } }
                }
            };

            _aggregatedOrderbook.ConsumeEvent(this, changeEventArgs);

            _bestPriceChannelMock.Verify(x => x.SendEvent(It.IsAny<object>(), It.Is<BestPriceChangeEventArgs>(price => price.BidAskPair.Ask == 98 && price.BidAskPair.Bid == 99)), Times.Once);

            changeEventArgs = new OrderBookChangeEventArgs
            {
                Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>
                {
                    { "BTCUSD", new Dictionary<decimal, OrderBookLevel>
                    {
                        { 120, new OrderBookLevel {Instrument = "BTCUSD", Volume = 0, Price = 98, Direction = OrderDirection.Sell} },
                        { 119, new OrderBookLevel {Instrument = "BTCUSD", Volume = 5, Price = 119, Direction = OrderDirection.Sell} }
                    } }
                }
            };

            _aggregatedOrderbook.ConsumeEvent(this, changeEventArgs);

            _bestPriceChannelMock.Verify(x => x.SendEvent(It.IsAny<object>(), It.Is<BestPriceChangeEventArgs>(price => price.BidAskPair.Ask == 119 && price.BidAskPair.Bid == 99)), Times.Once);


            var buyList = _aggregatedOrderbook.GetBuy("BTCUSD");
            var sellList = _aggregatedOrderbook.GetSell("BTCUSD");

            Assert.IsTrue(buyList.Count == 1);
            Assert.IsTrue(sellList.Count == 1);
            Assert.IsTrue(buyList.First().Price == 99);
            Assert.IsTrue(sellList.First().Price == 119);
        }
    }
}
