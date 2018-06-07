using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IEmailService
    {
        Task SendMarginCallEmailAsync(IMarginTradingAccount account);
        Task SendStopOutEmailAsync(IMarginTradingAccount account);
        Task SendOvernightSwapEmailAsync(string email, OvernightSwapNotification overnightSwapNotification);
    }
}
