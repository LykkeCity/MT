using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core
{
	public class OvernightSwapCalculation : IOvernightSwapHistory, IOvernightSwapState
	{
		public string Key => GetKey(AccountId, Instrument, Direction);
		
		public string AccountId { get; set; }
		public string Instrument { get; set; }
		public OrderDirection? Direction { get; set; }
		public DateTime Time { get; set; }
		public List<string> OpenOrderIds { get; set; }
		public decimal Value { get; set; }
		public decimal SwapRate { get; set; }
		
		public bool IsSuccess { get; set; }
		public Exception Exception { get; set; }
		
		public static string GetKey(string accountId, string instrument, OrderDirection? direction) =>
			$"{accountId}_{instrument ?? ""}_{direction?.ToString() ?? ""}";

		public static OvernightSwapCalculation Create(IOvernightSwapState state)
		{
			return new OvernightSwapCalculation
			{
				AccountId = state.AccountId,
				Instrument = state.Instrument,
				Direction = state.Direction,
				Time = state.Time,
				OpenOrderIds = state.OpenOrderIds,
				Value = state.Value,
				SwapRate = state.SwapRate,
				IsSuccess = true
			};
		}
		
		public static OvernightSwapCalculation Create(string accountId, string instrument,
			List<string> orderIds, DateTime timestamp, bool isSuccess, Exception exception = null, 
			decimal value = default(decimal), decimal swapRate = default(decimal), OrderDirection? direction = null)
		{
			return new OvernightSwapCalculation
			{
				AccountId = accountId,
				Instrument = instrument,
				Time = timestamp,
				Value = value,
				OpenOrderIds = orderIds,
				Direction = direction,
				IsSuccess = isSuccess,
				Exception = exception,
				SwapRate = swapRate
			};
		}
	}
}