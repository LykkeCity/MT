// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Orders
{
    public class PositionsGroupCloseResponse
    {
        public PositionCloseResponse[] Responses { get; set; }
    }
}