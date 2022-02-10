// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;

namespace MarginTrading.Backend.Services.Services
{
    public interface IRfqService
    {
        void SavePriceRequestForSpecialLiquidation(GetPriceForSpecialLiquidationCommand command);

        void RejectPriceRequest(string operationId, string reason);

        void ApprovePriceRequest(string operationId, decimal? price);

        List<GetPriceForSpecialLiquidationCommand> GetAllRequest();
    }
}