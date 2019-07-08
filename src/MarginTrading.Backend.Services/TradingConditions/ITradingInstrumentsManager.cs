// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface ITradingInstrumentsManager
    {
        Task UpdateTradingInstrumentsCacheAsync(string id = null);
    }
}