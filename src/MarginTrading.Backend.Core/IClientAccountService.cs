using System.Threading.Tasks;
using MarginTrading.Backend.Core.Clients;

namespace MarginTrading.Backend.Core
{
    public interface IClientAccountService
    {
        Task<string> GetNotificationId(string clientId);
        Task<IClientAccount> GetAsync(string clientId);
    }
}
