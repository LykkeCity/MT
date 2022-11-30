// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Snow.Contracts.Responses;
using MarginTrading.Backend.Contracts.ErrorCodes;

namespace MarginTrading.Backend.Contracts.Orders
{
    public sealed class OrderPlaceResponse : ErrorCodeResponse<PlaceOrderError>
    {
        public string OrderId { get; set; }
    }
}