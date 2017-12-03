using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Frontend.Repositories.Contract;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.Frontend.Repositories.Entities
{
    public class MarginTradingWatchListEntity : TableEntity, IMarginTradingWatchList
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string Name { get; set; }
        public bool ReadOnly { get; set; }
        public int Order { get; set; }
        public string AssetIds { get; set; }

        List<string> IMarginTradingWatchList.AssetIds
            => AssetIds.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();

        public static string GeneratePartitionKey(string accountId)
        {
            return accountId;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static MarginTradingWatchListEntity Create(IMarginTradingWatchList src)
        {
            return new MarginTradingWatchListEntity
            {
                PartitionKey = GeneratePartitionKey(src.ClientId),
                RowKey = GenerateRowKey(src.Id),
                Id = src.Id,
                ClientId = src.ClientId,
                Name = src.Name,
                ReadOnly = src.ReadOnly,
                Order = src.Order,
                AssetIds = string.Join(",", src.AssetIds)
            };
        }
    }
}