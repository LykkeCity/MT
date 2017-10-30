namespace MarginTrading.Backend.Services.Events
{
    public interface IEventConsumer
    {
        /// <summary>
        /// Less ConsumerRank are called first
        /// </summary>
        int ConsumerRank { get; }
    }

    // ReSharper disable once TypeParameterCanBeVariant
    public interface IEventConsumer<TEventArgs> : IEventConsumer
    {
        void ConsumeEvent(object sender, TEventArgs ea);
    }

    // ReSharper disable once TypeParameterCanBeVariant
}