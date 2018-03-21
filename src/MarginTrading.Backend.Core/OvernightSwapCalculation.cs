using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
	public class OvernightSwapCalculation : IOvernightSwapHistory, IOvernightSwapState
	{
		public string Key => GetKey(AccountId, Instrument, Direction);
		
		public string AccountId { get; set; }
		public string Instrument { get; set; }
		public OrderDirection? Direction { get; set; }
		public DateTime Timestamp { get; set; }
		public List<string> OpenOrderIds { get; set; }
		public decimal Value { get; set; }
		public decimal SwapRate { get; set; }
		
		public bool IsSuccess { get; set; }
		public Exception Exception { get; set; }
		
		public static string GetKey(string accountId, string instrument, OrderDirection? direction) =>
			string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(instrument) || direction == null
				? throw new Exception($"One of fields is invalid. Account id: {accountId}, instrument: {instrument}, direction: {direction}.")
				: $"{accountId}_{instrument}_{direction}";

		public static OvernightSwapCalculation Create(IOvernightSwapState state)
		{
			return new OvernightSwapCalculation
			{
				AccountId = state.AccountId,
				Instrument = state.Instrument,
				Direction = state.Direction,
				Timestamp = state.Timestamp,
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
				Timestamp = timestamp,
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