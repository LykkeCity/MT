// Copyright (c) 2019 Lykke Corp.

using System.Threading.Tasks;
using Lykke.SlackNotifications;

namespace MarginTrading.Common.Services
{
    public interface IMtSlackNotificationsSender : ISlackNotificationsSender
    {
        Task SendRawAsync(string type, string sender, string message);
    }
}