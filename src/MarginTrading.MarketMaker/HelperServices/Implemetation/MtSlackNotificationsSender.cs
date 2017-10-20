using System.Threading.Tasks;
using Lykke.SlackNotifications;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    public class MtSlackNotificationsSender : ISlackNotificationsSender
    {
        private readonly ISlackNotificationsSender _sender;
        private readonly string _appName;
        private readonly string _channelType;

        public MtSlackNotificationsSender(ISlackNotificationsSender sender, string appName, string channelType)
        {
            _sender = sender;
            _appName = appName;
            _channelType = channelType;
        }

        public async Task SendAsync(string type, string sender, string message)
        {
            await _sender.SendAsync(_channelType, sender, GetSlackMsg(message));
        }

        private string GetSlackMsg(string message)
        {
            const string delim = "\n====================================\n";
            return $"{_appName}{delim}{message}{delim}";
        }
    }
}
