namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <summary>
    /// Sends cqrs messages from Trading Engine contexts
    /// </summary>
    public interface ICqrsSender
    {
        void SendCommandToAccountManagement<T>(T command);
        void SendCommandToSettingsService<T>(T command);
        void PublishEvent<T>(T ev);
    }
}