namespace MarginTrading.Core
{
	public class Quote : IQuote
	{
		public string Instrument { get; set; }

		public OrderDirection Direction { get; set; }

		public double Price { get; set; }
	}
}