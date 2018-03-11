namespace MarginTrading.Backend.Services.Events
{
    public interface IEventConsumer
    {
        /// <summary>
        /// Less ConsumerRank are called first
        /// </summary>
        int ConsumerRank { get; }
    }

    public interface IEventConsumer<in TEventArgs> : IEventConsumer
    {
        void ConsumeEvent(object sender, TEventArgs ea);
    }
}