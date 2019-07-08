// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Notifications
{
    public class SlackNotificationRequestMsg
    {
        public string Sender { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }

    public interface ISlackNotificationsProducer
    {
        Task SendNotification(string type, string message, string sender);
    }
}
