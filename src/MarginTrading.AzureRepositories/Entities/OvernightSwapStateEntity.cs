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
		public DateTime Time { get; set; }
		public string OpenOrderIds { get; set; }
		List<string> IOvernightSwapState.OpenOrderIds => JsonConvert.DeserializeObject<List<string>>(OpenOrderIds);
		public decimal Value { get; set; }
		public decimal SwapRate { get; set; }
		
		public static string GetKey(string accountId, string instrument, OrderDirection? direction) =>
			$"{accountId}_{instrument ?? ""}_{direction?.ToString() ?? ""}";
		
		public static OvernightSwapStateEntity Create(IOvernightSwapState obj)
		{
			return new OvernightSwapStateEntity
			{
				PartitionKey = obj.AccountId,
				RowKey = GetKey(obj.AccountId, obj.Instrument, obj.Direction),
				AccountId = obj.AccountId,
				Instrument = obj.Instrument,
				Direction = obj.Direction?.ToString(),
				Time = obj.Time,
				OpenOrderIds = JsonConvert.SerializeObject(obj.OpenOrderIds),
				Value = obj.Value,
				SwapRate = obj.SwapRate
			};
		}
	}
}