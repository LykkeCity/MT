using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MarginTrading.Backend.Core;
using Newtonsoft.Json;

namespace MarginTrading.SqlRepositories
{
    public class SqlBlobRepository : IMarginTradingBlobRepository
    {
        private const string TableName = "BlobData";
        private const string CreateTableScript = @"CREATE TABLE [{0}](
[BlobKey] [nvarchar] (64) NOT NULL PRIMARY KEY, 
[Data] [nvarchar] (MAX) NULL, 
[Timestamp] [DateTime] NULL 
);";
        
        private readonly string _connectionString;
        
        public SqlBlobRepository(string connectionString)
        {
            _connectionString = connectionString;
            
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.CreateTableIfDoesntExists(CreateTableScript, TableName);
            }
        }
        
        public T Read<T>(string blobContainer, string key)
        {
            return ReadAsync<T>(blobContainer, key).GetAwaiter().GetResult();
        }

        public async Task<T> ReadAsync<T>(string blobContainer, string key)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var data = (await conn.QueryAsync<string>(
                    $"SELECT Data FROM {TableName} WHERE BlobKey=@blobKey",
                    new {blobKey = $"{blobContainer}_{key}"})).SingleOrDefault();

                if (string.IsNullOrEmpty(data))
                    return default(T);
                
                return JsonConvert.DeserializeObject<T>(data); 
            }
        }

        public (T, DateTime) ReadWithTimestamp<T>(string blobContainer, string key)
        {
            return ReadWithTimestampAsync<T>(blobContainer, key).GetAwaiter().GetResult();
        }

        public async Task<(T, DateTime)> ReadWithTimestampAsync<T>(string blobContainer, string key)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var (value, timestamp) = (await conn.QueryAsync<(string value, DateTime timestamp)>(
                    $"SELECT Data, Timestamp FROM {TableName} WHERE BlobKey=@blobKey",
                    new {blobKey = $"{blobContainer}_{key}"})).SingleOrDefault();

                return string.IsNullOrEmpty(value)
                    ? (Activator.CreateInstance<T>(), SqlDateTime.MinValue.Value)
                    : (JsonConvert.DeserializeObject<T>(value), timestamp);
            }
        }

        public async Task Write<T>(string blobContainer, string key, T obj)
        {
            var request = new
            {
                data = JsonConvert.SerializeObject(obj),
                blobKey = $"{blobContainer}_{key}",
                timestamp = DateTime.Now
            };
            
            using (var conn = new SqlConnection(_connectionString))
            {
                try
                {
                    await conn.ExecuteAsync(
                        $"insert into {TableName} (BlobKey, Data, Timestamp) values (@blobKey, @data, @timestamp)",
                        request);
                }
                catch
                {
                    await conn.ExecuteAsync(
                        $"update {TableName} set Data=@data, Timestamp = @timestamp where BlobKey=@blobKey",
                        request);
                }
            }
        }
    }
}
