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

        /// <summary>
        /// Getting paginated RFQ list with pause summary with filters applied.
        /// Remark: Response.TotalSize is not quite correct here cause it doesn't take into account pause state filtration.
        /// Since pause state filters are being applied application side but totalSize is being calculated DB server
        /// side there will always be a chance for discrepancy. In other words, totalSize here means total amount of
        /// items matching almost all filters except pause state filters described in <see cref="GetApplyPauseFilterFunc"/>
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        Task<PaginatedResponse<RfqWithPauseSummary>> GetAsync(RfqFilter filter, int skip, int take);
    }
}