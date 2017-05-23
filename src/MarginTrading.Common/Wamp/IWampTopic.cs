using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Documentation;
using MarginTrading.Core;

namespace MarginTrading.Common.Wamp
{
    public interface IWampTopic
    {
        [DocMe(Name = "prices.update", Description = "sends prices")]
        InstrumentBidAskPair PricesUpdate();
        [DocMe(Name = "user.notificationId", Description = "sends user updates on position, account changes and dictionaries")]
        NotifyResponse UserUpdates();

    }
}
