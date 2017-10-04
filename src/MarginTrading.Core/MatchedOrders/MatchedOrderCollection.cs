using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;

namespace MarginTrading.Core.MatchedOrders
{
    [JsonConverter(typeof(MatchedOrderCollectionConverter))]
    public class MatchedOrderCollection : IReadOnlyCollection<MatchedOrder>
    {
        private IReadOnlyList<MatchedOrder> _items;

        public decimal SummaryVolume { get; private set; }
        public decimal WeightedAveragePrice { get; private set; }

        public IReadOnlyList<MatchedOrder> Items
        {
            get => _items;
            private set
            {
                _items = value;

                SummaryVolume = _items.Sum(item => Math.Abs(item.Volume));

                if (SummaryVolume > 0)
                {
                    WeightedAveragePrice = _items.Sum(x => x.Price * Math.Abs(x.Volume)) / SummaryVolume;
                }
            }
        }

        public MatchedOrderCollection(IEnumerable<MatchedOrder> orders = null)
        {
            Items = orders?.ToList() ?? new List<MatchedOrder>();
        }

        public void Add(MatchedOrder order)
        {
            AddRange(new[] {order});
        }

        public void AddRange(IEnumerable<MatchedOrder> orders)
        {
            Items = Items.Union(orders).ToImmutableList();
        }


        #region IReadOnlyCollection

        public IEnumerator<MatchedOrder> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int Count => _items.Count;

        #endregion

    }
}