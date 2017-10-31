using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Core.Mappers
{
    public static class DomainToRabbitMqContractMapper
    {
        public static BidAskPairRabbitMqContract ToRabbitMqContract(this InstrumentBidAskPair pair)
        {
            return new BidAskPairRabbitMqContract
            {
                Instrument = pair.Instrument,
                Ask = pair.Ask,
                Bid = pair.Bid,
                Date = pair.Date
            };
        }
    }
}