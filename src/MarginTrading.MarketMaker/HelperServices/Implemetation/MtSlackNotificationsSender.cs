using System.Threading.Tasks;
using Lykke.SlackNotifications;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    public class MtSlackNotificationsSender : ISlackNotificationsSender
    {
        private readonly ISlackNotificationsSender _sender;
        private readonly string _appName;

        public MtSlackNotificationsSender(ISlackNotificationsSender sender, string appName)
        {
            _sender = sender;
            _appName = appName;
        }

        public async Task SendAsync(string type, string sender, string message)
        {
            await _sender.SendAsync("MarginTrading", sender, GetSlackMsg(message));
        }

        private string GetSlackMsg(string message)
        {
            const string delim = "\n====================================\n";
            return $"{_appName}{delim}{message}{delim}";
        }
    }
}
