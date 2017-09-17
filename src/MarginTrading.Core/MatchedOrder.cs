using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;

namespace MarginTrading.Core
{

    public class MatchedOrder
    {
        public string OrderId { get; set; }
        public string MarketMakerId { get; set; }
        public double LimitOrderLeftToMatch { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public string ClientId { get; set; }
        public DateTime MatchedDate { get; set; }

        public static MatchedOrder Create(MatchedOrder src)
        {
            return new MatchedOrder
            {
                OrderId = src.OrderId,
                MarketMakerId = src.MarketMakerId,
                LimitOrderLeftToMatch = src.LimitOrderLeftToMatch,
                Volume = src.Volume,
                Price = src.Price,
                ClientId = src.ClientId,
                MatchedDate = src.MatchedDate
            };
        }
    }

    [JsonConverter(typeof(MatchedOrderCollectionConverter))]
    public class MatchedOrderCollection : IReadOnlyCollection<MatchedOrder>
    {
        private IReadOnlyList<MatchedOrder> _items;

        public double SummaryVolume { get; private set; }
        public double WeightedAveragePrice { get; private set; }

        public IReadOnlyList<MatchedOrder> Items
        {
            get => _items;
            set
            {
                _items = value;

                SummaryVolume = _items.Sum(item => item.Volume);

                if (SummaryVolume > 0)
                {
                    WeightedAveragePrice = _items.Sum(x => x.Price * x.Volume) / SummaryVolume;
                }
            }
        }

        public MatchedOrderCollection(IReadOnlyList<MatchedOrder> orders = null)
        {
            Items = orders ?? new List<MatchedOrder>();
        }

        public static implicit operator MatchedOrderCollection(List<MatchedOrder> orders)
        {
            return new MatchedOrderCollection(orders);
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

    public class MatchedOrderCollectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MatchedOrderCollection);
        }

        public override object ReadJson(
            JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer)
        {
            var list = reader != null ? serializer.Deserialize<List<MatchedOrder>>(reader) : null;
            return new MatchedOrderCollection(list);
        }

        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            var collection = (MatchedOrderCollection) value;
            serializer.Serialize(writer, collection.Items);
        }
    }

    public static class MatchedOrderExtension
    {
        public static LimitOrder CreateLimit(this MatchedOrder order, string instrument, OrderDirection direction)
        {
            return new LimitOrder
            {
                MarketMakerId = order.MarketMakerId,
                Instrument = instrument,
                Price = order.Price,
                Volume = direction == OrderDirection.Buy ? order.LimitOrderLeftToMatch : -order.LimitOrderLeftToMatch
            };
        }
    }
}