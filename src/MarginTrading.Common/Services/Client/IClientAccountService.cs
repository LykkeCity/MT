using System.Threading.Tasks;

namespace MarginTrading.Common.Services.Client
{
    public interface IClientAccountService
    {
        Task<string> GetNotificationId(string clientId);
        Task<string> GetEmail(string clientId);
        Task<bool> IsPushEnabled(string clientId);
    }
}
