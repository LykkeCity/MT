using System.Collections.Generic;
using Common;
using MarginTrading.MarketMaker.Enums;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories.Entities
{
    internal class AssetPairSettingsEntity : TableEntity
    {
        public AssetPairSettingsEntity()
        {
            PartitionKey = "AssetSettings";
        }


        public string AssetName
        {
            get => RowKey;
            set => RowKey = value;
        }

        /// <summary>
        /// Quotes source type
        /// </summary>
        public AssetPairQuotesSourceTypeEnum QuotesSourceType { get; set; }

        /// <summary>
        /// External exchange which will be used for getting quotes, if <see cref="QuotesSourceType"/> is set to <see cref="AssetPairQuotesSourceTypeEnum.External"/>
        /// </summary>
        public string ExternalExchange { get; set; }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return new Dictionary<string, EntityProperty>
            {
                {nameof(PartitionKey), new EntityProperty(PartitionKey) },
                {nameof(RowKey), new EntityProperty(AssetName) },
                {nameof(QuotesSourceType), new EntityProperty(QuotesSourceType.ToString()) },
                {nameof(ExternalExchange), new EntityProperty(ExternalExchange) },
            };
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            AssetName = properties[nameof(RowKey)].StringValue;
            QuotesSourceType = properties[nameof(QuotesSourceType)].StringValue.ParseEnum<AssetPairQuotesSourceTypeEnum>();
            ExternalExchange = properties[nameof(ExternalExchange)].StringValue;
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