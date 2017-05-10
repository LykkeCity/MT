using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingBlobRepository
    {
        T Read<T>(string blobContainer, string key);
        Task Write<T>(string blobContainer, string key, T obj);
    }
}
