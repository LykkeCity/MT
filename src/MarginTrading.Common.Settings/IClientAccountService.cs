using System.Threading.Tasks;
using MarginTrading.Common.Settings.Repositories;

namespace MarginTrading.Common.Settings
{
    public interface IClientAccountService
    {
        Task<string> GetNotificationId(string clientId);
        Task<IClientAccount> GetAsync(string clientId);
    }
}
