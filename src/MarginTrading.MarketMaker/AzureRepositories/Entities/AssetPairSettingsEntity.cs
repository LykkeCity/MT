using System;
using MarginTrading.MarketMaker.Enums;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories.Entities
{
    internal class AssetPairSettingsEntity : TableEntity
    {
        public string AssetName { get; set; }

        public AssetPairQuotesSourceEnum PairQuotesSourceEnum { get; set; }

        public string QuotesSource
        {
            get => PairQuotesSourceEnum.ToString();
            set => PairQuotesSourceEnum = (AssetPairQuotesSourceEnum) Enum.Parse(typeof(AssetPairQuotesSourceEnum), value, true);
        }

        public static string GeneratePartitionKey() => "AssetSettings";

        public static string GenerateRowKey(string assetName) => assetName;
    }
}
