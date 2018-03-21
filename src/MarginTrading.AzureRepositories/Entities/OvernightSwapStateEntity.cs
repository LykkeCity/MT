using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace MarginTrading.AzureRepositories.Entities
{
	public class OvernightSwapStateEntity : TableEntity, IOvernightSwapState
	{
		public string AccountId { get; set; }
		public string Instrument { get; set; }
		public string Direction { get; set; }
		OrderDirection? IOvernightSwapState.Direction => 
			Enum.TryParse<OrderDirection>(Direction, out var direction) ? direction : (OrderDirection?)null;
		public DateTime Timestamp { get; set; }
		public string OpenOrderIds { get; set; }
		List<string> IOvernightSwapState.OpenOrderIds => JsonConvert.DeserializeObject<List<string>>(OpenOrderIds);
		public decimal Value { get; set; }
		public decimal SwapRate { get; set; }
		
		public static OvernightSwapStateEntity Create(IOvernightSwapState obj)
		{
			return new OvernightSwapStateEntity
			{
				PartitionKey = obj.AccountId,
				AccountId = obj.AccountId,
				Instrument = obj.Instrument,
				Direction = obj.Direction?.ToString(),
				Timestamp = obj.Timestamp,
				OpenOrderIds = JsonConvert.SerializeObject(obj.OpenOrderIds),
				Value = obj.Value,
				SwapRate = obj.SwapRate
			};
		}
	}
}