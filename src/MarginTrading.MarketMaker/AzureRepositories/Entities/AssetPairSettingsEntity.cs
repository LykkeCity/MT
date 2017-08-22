using Common;
using MarginTrading.MarketMaker.Enums;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories.Entities
{
    internal class AssetPairSettingsEntity : TableEntity
    {
        public string AssetName
        {
            get => RowKey;
            set => RowKey = value;
        }

        public AssetPairQuotesSourceEnum QuotesSourceEnum { get; set; }

        public string QuotesSource
        {
            get => QuotesSourceEnum.ToString();
            set => QuotesSourceEnum = value.ParseEnum<AssetPairQuotesSourceEnum>();
        }

        public static string GeneratePartitionKey()
        {
            return "AssetSettings";
        }

        public static string GenerateRowKey(string assetName)
        {
            return assetName;
        }
    }
}