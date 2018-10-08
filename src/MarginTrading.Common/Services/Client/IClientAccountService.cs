using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.Models;

namespace MarginTrading.Common.Services.Client
{
    public interface IClientAccountService
    {
        Task<string> GetNotificationId(string clientId);
        Task<string> GetEmail(string clientId);
        Task<bool> IsPushEnabled(string clientId);
        Task<ClientModel> GetClientAsync(string clientId);
    }
}
