using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;
using MarginTrading.Core.MatchedOrders;

namespace MarginTrading.Services.Events
{
    public class OrderBookChangeEventArgs
    {
        public long MessageId { get; set; }
        public Dictionary<string, Dictionary<decimal, OrderBookLevel>> Buy = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>();
        public Dictionary<string, Dictionary<decimal, OrderBookLevel>> Sell = new Dictionary<string, Dictionary<decimal, OrderBookLevel>>();

        public void AddOrderBookLevel(OrderBookLevel level)
        {
            var dict = level.Direction == OrderDirection.Buy ? Buy : Sell;

            if (!dict.ContainsKey(level.Instrument))
                dict.Add(level.Instrument, new Dictionary<decimal, OrderBookLevel>());

            var levelDict = dict[level.Instrument];

            if (levelDict.ContainsKey(level.Price))
                levelDict[level.Price] = level;
            else 
                levelDict.Add(level.Price, level);
        }

        public void AddOrderBookLevels(params LimitOrder[] orders)
        {
            foreach (var order in orders)
                AddOrderBookLevel(OrderBookLevel.Create(order));
        }

        public void AddOrderBookLevels(OrderDirection direction, string instrument, params MatchedOrder[] orders)
        {
            foreach (var order in orders)
                AddOrderBookLevel(OrderBookLevel.Create(order, direction, instrument));
        }

        public void AddOrderBookLevelsToDelete(params LimitOrder[] orders)
        {
            foreach (var order in orders)
                AddOrderBookLevel(OrderBookLevel.CreateDeleted(order));
        }

        public void AddOrderBookLevelsToDeletePartial(params LimitOrder[] orders)
        {
            foreach (var order in orders)
                AddOrderBookLevel(OrderBookLevel.Create(order));
        }

        public bool HasEvents()
        {
            return Buy.Count > 0 || Sell.Count > 0;
        }

        public IEnumerable<string> GetChangedInstruments()
        {
            List<string> instruments = Buy.Keys.ToList();
            instruments.AddRange(Sell.Keys.ToList());

            return instruments.Distinct();
        }
    }
}