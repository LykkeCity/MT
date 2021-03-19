// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services
{
    public interface IValidateOrderService
    {
        Task<(Order order, List<Order> relatedOrders)> ValidateRequestAndCreateOrders(OrderPlaceRequest request);

        void MakePreTradeValidation(Order order, bool shouldOpenNewPosition, IMatchingEngineBase matchingEngine,
            decimal additionalMargin);

        void ValidateOrderPriceChange(Order order, decimal newPrice);

        Task<OrderInitialParameters> GetOrderInitialParameters(string assetPairId, string accountId);

        IAssetPair GetAssetPairIfAvailableForTrading(string assetPairId, OrderType orderType,
            bool shouldOpenNewPosition, bool isPreTradeValidation);
        
        bool CheckIfPendingOrderExecutionPossible(string assetPairId, OrderType orderType, bool shouldOpenNewPosition);
        void ValidateValidity(DateTime? validity, OrderType orderType);
        void ValidateForceOpenChange(Order order, bool? forceOpen);
    }
}
