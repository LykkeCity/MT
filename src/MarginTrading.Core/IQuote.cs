namespace MarginTrading.Core
{
	public interface IQuote
	{
		string Instrument { get; set; }

		OrderDirection Direction { get; set; }

		double Price { get; set; }
	}
}