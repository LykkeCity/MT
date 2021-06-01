// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Rfq;
using Refit;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// API for performing operations with RFQ
    /// </summary>
    [PublicAPI]
    public interface IRfqApi
    {
        /// <summary>
        /// Returns RFQs
        /// </summary>
        [Get("/api/rfq")]
        Task<PaginatedResponseContract<RfqContract>> GetAsync([Query, CanBeNull] GetRfqRequest getRfqRequest, [Query] int skip = 0, [Query] int take = 20);
    }
}