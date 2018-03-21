using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
	public class OvernightSwapCalculation
	{
		public string AccountId { get; set; }
		public string Instrument { get; set; }
		public DateTime Timestamp { get; set; }
		public List<string> OpenOrderIds { get; set; }
		public decimal Value { get; set; }

		public string Key => $"{AccountId}_{Instrument}";
	}
}