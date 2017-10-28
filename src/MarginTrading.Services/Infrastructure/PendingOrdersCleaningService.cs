using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Core.Assets;

namespace MarginTrading.Services.Infrastructure
{
    public class PendingOrdersCleaningService : TimerPeriod
    {
        private readonly ILog _log;
        private readonly IOrderReader _orderReader;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAssetPairDayOffService _assetDayOffService;

        private bool _isCleanPerformed;

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
            if (!_assetDayOffService.IsPendingOrdersDisabledTime())
            {
                _isCleanPerformed = false;
                return Task.CompletedTask;
            }
                
            if (_isCleanPerformed)
                return Task.CompletedTask;

            _isCleanPerformed = true;

            var pendingOrders = _orderReader.GetPending();

            foreach (var pendingOrder in pendingOrders)
            {
                if (_assetDayOffService.IsAssetPairHasNoDayOff(pendingOrder.Instrument))
                    continue;
                
                try
                {
                    _tradingEngine.CancelPendingOrder(pendingOrder.Id, OrderCloseReason.CanceledBySystem);
                }
                catch (Exception e)
                {
                    _log.WriteErrorAsync(nameof(PendingOrdersCleaningService),
                        $"Cancelling pending order {pendingOrder.Id}", pendingOrder.ToJson(), e);
                }
            }

            return Task.CompletedTask;
        }
    }
}