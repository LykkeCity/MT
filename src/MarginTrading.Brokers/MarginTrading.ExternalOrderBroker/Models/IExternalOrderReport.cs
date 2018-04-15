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