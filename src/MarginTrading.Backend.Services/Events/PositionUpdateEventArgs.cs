// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services.Events
{
	public class PositionUpdateEventArgs
	{
		public string AssetId { get; set; }
		public string CounterpartyId { get; set; }
	}
}