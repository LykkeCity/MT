using System.Threading.Tasks;
using Lykke.SlackNotifications;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    public class MtMmRisksSlackNotificationsSender : IMtMmRisksSlackNotificationsSender
    {
        private readonly ISlackNotificationsSender _sender;

        public MtMmRisksSlackNotificationsSender(ISlackNotificationsSender sender)
        {
            _sender = sender;
        }

        public async Task SendAsync(string type, string sender, string message)
        {
            await _sender.SendAsync("MtMmRisks", sender, message);
        }
    }
}