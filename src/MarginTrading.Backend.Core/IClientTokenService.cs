using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IClientTokenService
    {
        Task<string> GetClientId(string token);
    }
}
