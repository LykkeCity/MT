using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IClientTokenService
    {
        Task<string> GetClientId(string token);
    }
}
