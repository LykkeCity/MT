using FluentScheduler;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Scheduling
{
    public class EodJob : IJob
    {
        private readonly ITradingEngine _tradingEngine;

        public EodJob(ITradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;
        }
        
        public void Execute()
        {
            _tradingEngine.ProcessExpiredOrders();
        }
    }
}