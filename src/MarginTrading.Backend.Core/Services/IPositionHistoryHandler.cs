// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Services
{
    public interface IPositionHistoryHandler
    {
        Task HandleOpenPosition(Position position, string additionalInfo, PositionOpenMetadata metadata);
        Task HandleClosePosition(Position position, DealContract deal, string additionalInfo);
        Task HandlePartialClosePosition(Position position, DealContract deal, string additionalInfo);
    }
}