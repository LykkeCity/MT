using System.Threading.Tasks;
using MarginTrading.Core.Clients;

namespace MarginTrading.Core
{
    public interface IClientAccountService
    {
        Task<string> GetNotificationId(string clientId);
        Task<IClientAccount> GetAsync(string clientId);
    }
}
