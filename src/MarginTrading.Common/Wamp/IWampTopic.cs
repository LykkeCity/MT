using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Documentation;
using MarginTrading.Core;

namespace MarginTrading.Common.Wamp
{
    public interface IWampTopic
    {
        [DocMe(Name = "prices.update", Description = "sends prices for all instruments")]
        InstrumentBidAskPair AllPricesUpdate();
        [DocMe(Name = "prices.update.{instrumentId}", Description = "sends prices for specific instrument")]
        InstrumentBidAskPair InstumentPricesUpdate();
        [DocMe(Name = "user.{notificationId}", Description = "sends user updates on position, account changes and dictionaries")]
        NotifyResponse UserUpdates();

    }
}
