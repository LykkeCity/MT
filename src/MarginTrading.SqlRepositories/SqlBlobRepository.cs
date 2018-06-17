using System.IO;
using System.Threading.Tasks;
using Lykke.SettingsReader;
using MarginTrading.Backend.Core;
using Newtonsoft.Json;

namespace MarginTrading.SqlRepositories
{
    public class SqlBlobRepository : IMarginTradingBlobRepository
    {
        public SqlBlobRepository()
        {
        }

        public T Read<T>(string blobContainer, string key)
        {
            var filename = $"{blobContainer}_{key}.json";

            if (File.Exists(filename))
            {
                var str = File.ReadAllText(filename);

                return JsonConvert.DeserializeObject<T>(str);
            }
//            if (_blobStorage.HasBlobAsync(blobContainer, key).Result)
//            {
//                var data = _blobStorage.GetAsync(blobContainer, key).Result.ToBytes();
//                var str = Encoding.UTF8.GetString(data);
//
//                return JsonConvert.DeserializeObject<T>(str);
//            }

            return default(T);
        }


        public Task<T> ReadAsync<T>(string blobContainer, string key)
        {
            return Task.FromResult(Read<T>(blobContainer, key));
        }

        public async Task Write<T>(string blobContainer, string key, T obj)
        {
            var filename = $"{blobContainer}_{key}.json";
            File.WriteAllText(filename, JsonConvert.SerializeObject(obj));
//            var data = JsonConvert.SerializeObject(obj).ToUtf8Bytes();
//            await _blobStorage.SaveBlobAsync(blobContainer, key, data);
        }
    }
}
