// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Sentiments;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/sentiments")]
    [ApiController]
    public class SentimentsController : ControllerBase, ISentimentsApi
    {
        private readonly ISentimentCache _sentimentCache;

        public SentimentsController(ISentimentCache sentimentCache)
        {
            _sentimentCache = sentimentCache;
        }
        
        [HttpGet]
        [Route("{productId}")]
        public Task<SentimentInfoContract> GetSentimentInfoAsync(string productId)
        {
            var (shortShare, longShare) = _sentimentCache.Get(productId);

            return Task.FromResult(new SentimentInfoContract
            {
                InstrumentId = productId,
                Sell = shortShare,
                Buy = longShare
            });
        }

        [HttpGet]
        public Task<List<SentimentInfoContract>> ListSentimentInfoAsync()
        {
            var response = _sentimentCache
                .GetAll()
                .Select(SentimentExtensions.ToContract)
                .ToList();

            return Task.FromResult(response);
        }

        [HttpPost("filtered")]
        public Task<List<SentimentInfoContract>> ListFilteredSentimentInfoAsync(SentimentInfoRequest request)
        {
            var response = _sentimentCache
                .GetFiltered(request.ProductIds)
                .Select(SentimentExtensions.ToContract)
                .ToList();

            return Task.FromResult(response);
        }
    }
}