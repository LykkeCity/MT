using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Contract.ClientContracts
{
    [DisplayName("Trade")]
    public class TradeClientContract
    {
        [DisplayName("Trade unique id")]
        public string Id { get; set; }

        [DisplayName("Order unique id")]
        public string OrderId { get; set; }
        
        [DisplayName("Instrument  unique id")]
        public string AssetPairId { get; set; }
        
        [DisplayName("Trade type on order's side: buy or sell")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TradeClientType Type { get; set; }
        
        [DisplayName("Trade date and time")]
        public DateTime Date { get; set; }

        [DisplayName("Trade effective price")]
        public decimal Price { get; set; }

        [DisplayName("Trade effective volume")]
        public decimal Volume { get; set; }
    }

    public enum TradeClientType
    {
        Buy = 0,
        Sell = 1
    }
}