using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core
{
	public class OvernightSwapCalculation : IOvernightSwapHistory, IOvernightSwapState
	{
		public string Key => GetKey(AccountId, Instrument, Direction);
		
		public string ClientId { get; set; }
		public string AccountId { get; set; }
		public string Instrument { get; set; }
		public OrderDirection? Direction { get; set; }
		public DateTime Time { get; set; }
		public decimal Volume { get; set; }
		public decimal Value { get; set; }
		public decimal SwapRate { get; set; }
		public List<string> OpenOrderIds { get; set; }
		
		public bool IsSuccess { get; set; }
		public Exception Exception { get; set; }
		
		public static string GetKey(string accountId, string instrument, OrderDirection? direction) =>
			$"{accountId}_{instrument ?? ""}_{direction?.ToString() ?? ""}";

		public static OvernightSwapCalculation Create(IOvernightSwapState state)
		{
			return new OvernightSwapCalculation
			{
				ClientId = state.ClientId,
				AccountId = state.AccountId,
				Instrument = state.Instrument,
				Direction = state.Direction,
				Time = state.Time,
				Volume = state.Volume,
				Value = state.Value,
				SwapRate = state.SwapRate,
				OpenOrderIds = state.OpenOrderIds,
				IsSuccess = true
			};
		}
		
		public static OvernightSwapCalculation Create(string clientId, string accountId, string instrument,
			List<string> orderIds, DateTime timestamp, bool isSuccess, Exception exception = null, decimal volume = default(decimal),
			decimal value = default(decimal), decimal swapRate = default(decimal), OrderDirection? direction = null)
		{
			return new OvernightSwapCalculation
			{
				ClientId = clientId,
				AccountId = accountId,
				Instrument = instrument,
				Direction = direction,
				Time = timestamp,
				Volume = volume,
				Value = value,
				SwapRate = swapRate,
				OpenOrderIds = orderIds,
				IsSuccess = isSuccess,
				Exception = exception,
			};
		}

		public static OvernightSwapCalculation Update(OvernightSwapCalculation newCalc, OvernightSwapCalculation lastCalc)
		{
			return new OvernightSwapCalculation
				{
					ClientId = newCalc.ClientId,
					AccountId = newCalc.AccountId,
					Instrument = newCalc.Instrument,
					Direction = newCalc.Direction,
					Time = newCalc.Time,
					Volume = newCalc.Volume,
					Value = newCalc.Value + lastCalc.Value,
					OpenOrderIds = newCalc.OpenOrderIds.Concat(lastCalc.OpenOrderIds).ToList(),
					SwapRate = newCalc.SwapRate,
					IsSuccess = true
				};
		}
	}
}