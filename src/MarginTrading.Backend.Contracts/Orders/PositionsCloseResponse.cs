// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Snow.Contracts.Responses;

namespace MarginTrading.Backend.Contracts.Orders
{
    public sealed class PositionCloseResponse : ErrorCodeResponse<PositionCloseError>
    {
        public string PositionId { get; set; }
        public PositionCloseResultContract Result { get; set; }
        public string OrderId { get; set; }
    }
}