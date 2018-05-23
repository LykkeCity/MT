using System.Collections.Generic;
using MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading;

namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain
{
    public interface IExchange
    {
        string Name { get; }

        IReadOnlyList<Instrument> Instruments { get; }
    }
}
