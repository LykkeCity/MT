// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.Backend.Services.Services
{
    public interface IRfqService
    {
        void SavePriceRequestForSpecialLiquidation(GetPriceForSpecialLiquidationCommand command);

        void RejectPriceRequest(string operationId, string reason);

        void ApprovePriceRequest(string operationId, decimal? price);

        List<GetPriceForSpecialLiquidationCommand> GetAllRequest();

        Task<PaginatedResponse<Rfq>> GetAsync(RfqFilter filter, int skip, int take);
    }
}