namespace MarginTrading.Core
{
	public class Position : IPosition
	{
		public string ClientId { get; set; }
		public string Asset { get; set; }
		public double Volume { get; set; }
	}
}
