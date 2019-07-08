// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Services.Events
{
	public class PositionUpdateEventArgs
	{
		public string AssetId { get; set; }
		public string CounterpartyId { get; set; }
	}
}