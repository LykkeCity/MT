using System.Threading.Tasks;

namespace MarginTrading.Frontend.Services
{
    public interface IClientTokenService
    {
        Task<string> GetClientId(string token);
    }
}
