using System.Text;
using System.Threading.Tasks;
using AzureStorage.Blob;
using Common;
using MarginTrading.Core;
using Newtonsoft.Json;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingBlobRepository : IMarginTradingBlobRepository
    {
        private readonly AzureBlobStorage _blobStorage;

        public MarginTradingBlobRepository(string connectionString)
        {
            _blobStorage = new AzureBlobStorage(connectionString);
        }

        public T Read<T>(string blobContainer, string key)
        {
            if (_blobStorage.HasBlobAsync(blobContainer, key).Result)
            {
                var data = _blobStorage.GetAsync(blobContainer, key).Result.ToBytes();
                var str = Encoding.UTF8.GetString(data);

                return JsonConvert.DeserializeObject<T>(str);
            }

            return default(T);
        }
            
        public async Task Write<T>(string blobContainer, string key, T obj)
        {
            byte[] data = JsonConvert.SerializeObject(obj).ToUtf8Bytes();
            await _blobStorage.SaveBlobAsync(blobContainer, key, data);
        }
    }
}
