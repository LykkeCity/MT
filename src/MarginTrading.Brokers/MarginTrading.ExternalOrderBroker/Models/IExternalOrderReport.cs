// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.ExternalOrderBroker.Models
{
	public interface IExternalOrderReport
	{
		string Instrument { get; }
		
		string Exchange { get; }
		
		string BaseAsset { get; }
		
		string QuoteAsset { get; }

		string Type { get; }

		System.DateTime Time { get; }

		double Price { get; }

		double Volume { get; }

		double Fee { get; }

		string Id { get; }

		string Status { get; }

		string Message { get; }
	}
}