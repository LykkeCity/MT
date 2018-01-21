namespace MarginTrading.Backend.Services.Infrastructure
{
    public interface IAlertSeverityLevelService
    {
        string GetSlackChannelType(EventTypeEnum eventType);
    }
}