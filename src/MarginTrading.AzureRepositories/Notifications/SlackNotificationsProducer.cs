using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using MarginTrading.Core.Notifications;

namespace MarginTrading.AzureRepositories.Notifications
{
    public class SlackNotificationsProducer : ISlackNotificationsProducer
    {
        private readonly IQueueExt _queueExt;

        public SlackNotificationsProducer(IQueueExt queueExt)
        {
            _queueExt = queueExt;
        }

        public Task SendNotification(string type, string message, string sender)
        {
            return
                _queueExt.PutRawMessageAsync(
                    new SlackNotificationRequestMsg {Message = message, Sender = sender, Type = type}.ToJson());
        }
    }
}
