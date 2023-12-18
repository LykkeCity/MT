// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Sentiments;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// API for getting information on product sentiments
    /// </summary>
    [PublicAPI]
    public interface ISentimentsApi
    {
        /// <summary>
        /// Get product sentiments
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        [Get("/api/sentiments/{productId}")]
        Task<SentimentInfoContract> GetSentimentInfoAsync(string productId);

        /// <summary>
        /// Get all product sentiments
        /// </summary>
        /// <returns></returns>
        [Get("/api/sentiments")]
        Task<List<SentimentInfoContract>> ListSentimentInfoAsync();
        
        /// <summary>
        /// Get filtered product sentiments
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Post("/api/sentiments/filtered")]
        Task<List<SentimentInfoContract>> ListFilteredSentimentInfoAsync([Body] SentimentInfoRequest request);
    }
}