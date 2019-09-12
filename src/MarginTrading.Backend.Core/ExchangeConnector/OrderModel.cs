// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.ExchangeConnector
{
    public class OrderModel
    {
        /// <summary>Initializes a new instance of the OrderModel class.</summary>
        public OrderModel()
        {
        }

        /// <summary>Initializes a new instance of the OrderModel class.</summary>
        /// <param name="tradeType">Possible values include: 'Unknown', 'Buy',
        /// 'Sell'</param>
        /// <param name="orderType">Possible values include: 'Unknown',
        /// 'Market', 'Limit'</param>
        /// <param name="timeInForce">Possible values include:
        /// 'GoodTillCancel', 'FillOrKill'</param>
        /// <param name="dateTime">Date and time must be in 5 minutes threshold
        /// from UTC now</param>
        public OrderModel(
            TradeType tradeType,
            OrderType orderType,
            TimeInForce timeInForce,
            double volume,
            DateTime dateTime,
            string exchangeName = null,
            string instrument = null,
            double? price = null,
            string orderId = null,
            TradeRequestModality modality = TradeRequestModality.Regular)
        {
            this.ExchangeName = exchangeName;
            this.Instrument = instrument;
            this.TradeType = tradeType;
            this.OrderType = orderType;
            this.TimeInForce = timeInForce;
            this.Price = price;
            this.Volume = volume;
            this.DateTime = dateTime;
            this.OrderId = orderId;
            this.Modality = modality;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "exchangeName")]
        public string ExchangeName { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "instrument")]
        public string Instrument { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'Unknown', 'Buy', 'Sell'
        /// </summary>
        [JsonProperty(PropertyName = "tradeType")]
        public TradeType TradeType { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'Unknown', 'Market', 'Limit'
        /// </summary>
        [JsonProperty(PropertyName = "orderType")]
        public OrderType OrderType { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'GoodTillCancel',
        /// 'FillOrKill'
        /// </summary>
        [JsonProperty(PropertyName = "timeInForce")]
        public TimeInForce TimeInForce { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public double? Price { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "volume")]
        public double Volume { get; set; }

        /// <summary>
        /// Gets or sets date and time must be in 5 minutes threshold from UTC
        /// now
        /// </summary>
        [JsonProperty(PropertyName = "dateTime")]
        public DateTime DateTime { get; set; }

        public string OrderId { get; set; }

        [DefaultValue(TradeRequestModality.Regular)]
        public TradeRequestModality Modality { get; set; }
    }
}