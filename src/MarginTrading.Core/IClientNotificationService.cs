using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IClientNotificationService
    {
        Task<string> GetNotificationId(string clientId);
    }
}
