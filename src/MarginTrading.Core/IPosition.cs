namespace MarginTrading.Core
{
	public interface IPosition
	{
		string ClientId { get; set; }
		string Asset { get; set; }
		double Volume { get; set; }
	}
}
