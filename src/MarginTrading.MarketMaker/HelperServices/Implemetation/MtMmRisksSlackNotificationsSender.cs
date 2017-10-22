using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.SlackNotifications;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    public class MtMmRisksSlackNotificationsSender: MtSlackNotificationsSender, IMtMmRisksSlackNotificationsSender
    {
        public MtMmRisksSlackNotificationsSender(ISlackNotificationsSender sender, string appName) : base(sender, appName, "MtMmRisks")
        {
        }
    }
}
