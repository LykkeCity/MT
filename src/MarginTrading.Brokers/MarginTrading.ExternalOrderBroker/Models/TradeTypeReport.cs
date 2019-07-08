// Copyright (c) 2019 Lykke Corp.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.ExternalOrderBroker.Models
{
	[JsonConverter(typeof(StringEnumConverter))]
	public enum TradeTypeReport
	{
		[EnumMember(Value = "Unknown")] Unknown,
		[EnumMember(Value = "Buy")] Buy,
		[EnumMember(Value = "Sell")] Sell
	}
}