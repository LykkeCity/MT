using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class PendingOrdersCleaningService : TimerPeriod
    {
        private readonly ILog _log;
        private readonly IOrderReader _orderReader;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAssetPairDayOffService _assetDayOffService;

        public PendingOrdersCleaningService(ILog log, IOrderReader orderReader, ITradingEngine tradingEngine,
            IAssetPairDayOffService assetDayOffService)
            : base(nameof(PendingOrdersCleaningService), 60000, log)
        {
            _log = log;
            _orderReader = orderReader;
            _tradingEngine = tradingEngine;
            _assetDayOffService = assetDayOffService;
        }

        public override Task Execute()
        {
            var pendingOrders = _orderReader.GetPending().GroupBy(o => o.Instrument);
            foreach (var gr in pendingOrders)
            {
                if (!_assetDayOffService.ArePendingOrdersDisabled(gr.Key))
                    continue;

                foreach (var pendingOrder in gr)
                {
                    try
                    {
                        _tradingEngine.CancelPendingOrder(pendingOrder.Id, OrderCloseReason.CanceledBySystem, "Day off started");
                    }
                    catch (Exception e)
                    {
                        _log.WriteErrorAsync(nameof(PendingOrdersCleaningService),
                            $"Cancelling pending order {pendingOrder.Id}", pendingOrder.ToJson(), e);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}