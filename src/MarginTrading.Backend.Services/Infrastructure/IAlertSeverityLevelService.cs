// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Services.Infrastructure
{
    public interface IAlertSeverityLevelService
    {
        string GetSlackChannelType(EventTypeEnum eventType);
    }
}