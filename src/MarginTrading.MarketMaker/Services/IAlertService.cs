using System.Threading.Tasks;
using MarginTrading.MarketMaker.Enums;

namespace MarginTrading.MarketMaker.Services
{
    public interface IAlertService
    {
        void AlertPrimaryExchangeSwitched(string assetPairId, string newPrimaryExchange, ExchangeErrorState state, decimal preference);
        void AlertStopNewTrades(string assetPairId);
        void AlertRiskOfficer(string message, object context);
        void AlertStarted();
        Task AlertStopping();
    }
}