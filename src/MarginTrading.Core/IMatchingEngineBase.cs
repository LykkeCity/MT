namespace MarginTrading.Core
{
    public interface IMatchingEngineBase
    {
        string Id { get; }
    }

    public class MatchingEngineBase : IMatchingEngineBase
    {
        public string Id { get; set; }
    }
}
