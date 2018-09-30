using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.Assets
{
    public interface IAssetsManager
    {
        Task UpdateCacheAsync();
    }
}