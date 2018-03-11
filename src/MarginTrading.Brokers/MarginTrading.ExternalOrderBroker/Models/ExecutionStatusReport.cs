using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.ExternalOrderBroker.Models
{
	[JsonConverter(typeof(StringEnumConverter))]
	public enum ExecutionStatusReport
	{
		[EnumMember(Value = "Unknown")] Unknown,
		[EnumMember(Value = "Fill")] Fill,
		[EnumMember(Value = "PartialFill")] PartialFill,
		[EnumMember(Value = "Cancelled")] Cancelled,
		[EnumMember(Value = "Rejected")] Rejected,
		[EnumMember(Value = "New")] New,
		[EnumMember(Value = "Pending")] Pending
	}
}