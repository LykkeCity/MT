// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Snow.Contracts.Responses;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class PositionsGroupCloseResponse : ErrorCodeResponse<PositionGroupCloseError>
    {
        public PositionCloseResponse[] Responses { get; set; }
        
        public static PositionsGroupCloseResponse Ok(PositionCloseResponse[] responses)
        {
            return new PositionsGroupCloseResponse
            {
                Responses = responses,
                ErrorCode = PositionGroupCloseError.None
            };
        }
        
        public static PositionsGroupCloseResponse Fail(PositionGroupCloseError errorCode)
        {
            return new PositionsGroupCloseResponse
            {
                Responses = Array.Empty<PositionCloseResponse>(),
                ErrorCode = errorCode
            };
        }
    }
}