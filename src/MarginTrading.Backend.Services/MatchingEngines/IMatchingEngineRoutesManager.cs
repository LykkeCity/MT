// Copyright (c) 2019 Lykke Corp.

using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public interface IMatchingEngineRoutesManager
    {
        Task UpdateRoutesCacheAsync();
        IMatchingEngineRoute FindRoute(string clientId, string tradingConditionId, string instrumentId, OrderDirection orderType);
        Task HandleRiskManagerCommand(MatchingEngineRouteRisksCommand command);
        Task HandleRiskManagerBlockTradingCommand(MatchingEngineRouteRisksCommand command);
    }
}