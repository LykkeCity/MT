using System.Threading.Tasks;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Messages;

namespace MarginTrading.MarketMaker.Services
{
    public interface IAlertService
    {
        void AlertPrimaryExchangeSwitched(PrimaryExchangeSwitchedMessage message);
        void AlertRiskOfficer(string message);
        void AlertStarted();
        Task AlertStopping();
        void StopOrAllowNewTrades(string assetPairId, string reason, bool stop);
    }
}