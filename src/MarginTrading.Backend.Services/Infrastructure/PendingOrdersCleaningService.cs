// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class PendingOrdersCleaningService : TimerPeriod
    {
        private readonly ILog _log;
        private readonly IOrderReader _orderReader;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IIdentityGenerator _identityGenerator;

        public PendingOrdersCleaningService(ILog log, IOrderReader orderReader, ITradingEngine tradingEngine,
            IAssetPairDayOffService assetDayOffService, IIdentityGenerator identityGenerator)
            : base(nameof(PendingOrdersCleaningService), 60000, log)
        {
            _log = log;
            _orderReader = orderReader;
            _tradingEngine = tradingEngine;
            _assetDayOffService = assetDayOffService;
            _identityGenerator = identityGenerator;
        }

        //TODO: add setting
        public override Task Execute()
        {
            return Task.CompletedTask;
            
            //TODO: add flag to settings in MTC-155
//            var pendingOrders = _orderReader.GetPending().GroupBy(o => o.AssetPairId);
//            foreach (var gr in pendingOrders)
//            {
//                //if (!_assetDayOffService.ArePendingOrdersDisabled(gr.Key))
//                //    continue;
//
//                foreach (var pendingOrder in gr)
//                {
//                    try
//                    {
//                        _tradingEngine.CancelPendingOrder(pendingOrder.Id, OriginatorType.System, "Day off started", 
//                            _identityGenerator.GenerateGuid());
//                    }
//                    catch (Exception e)
//                    {
//                        _log.WriteErrorAsync(nameof(PendingOrdersCleaningService),
//                            $"Cancelling pending order {pendingOrder.Id}", pendingOrder.ToJson(), e);
//                    }
//                }
//            }
//
//            return Task.CompletedTask;
        }
    }
}