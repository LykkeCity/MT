using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace MarginTrading.AzureRepositories.Entities
{
	public class OvernightSwapHistoryEntity : TableEntity, IOvernightSwapHistory
	{
		public string AccountId { get; set; }
		public string Instrument { get; set; }
		public string Direction { get; set; }
		OrderDirection? IOvernightSwapState.Direction => 
			Enum.TryParse<OrderDirection>(Direction, out var direction) ? direction : (OrderDirection?)null;
		public DateTime Time { get; set; }
		public string OpenOrderIds { get; set; }
		List<string> IOvernightSwapState.OpenOrderIds => JsonConvert.DeserializeObject<List<string>>(OpenOrderIds);
		public double Value { get; set; }
		decimal IOvernightSwapState.Value => (decimal) Value;
		public double SwapRate { get; set; }
		decimal IOvernightSwapState.SwapRate => (decimal) SwapRate;
		
		public bool IsSuccess { get; set; }
		public string Exception { get; set; }
		Exception IOvernightSwapHistory.Exception => JsonConvert.DeserializeObject<Exception>(Exception);
		
		public static OvernightSwapHistoryEntity Create(IOvernightSwapHistory obj)
		{
			return new OvernightSwapHistoryEntity
			{
				PartitionKey = obj.AccountId,
				RowKey = $"{obj.Time:O}",
				AccountId = obj.AccountId,
				Instrument = obj.Instrument,
				Direction = obj.Direction?.ToString(),
				Time = obj.Time,
				OpenOrderIds = JsonConvert.SerializeObject(obj.OpenOrderIds),
				Value = (double) obj.Value,
				SwapRate = (double) obj.SwapRate,
				IsSuccess = obj.IsSuccess,
				Exception = JsonConvert.SerializeObject(obj.Exception)
			};
		}
	}
}