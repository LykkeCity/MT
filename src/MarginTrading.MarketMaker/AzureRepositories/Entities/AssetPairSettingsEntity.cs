using System.Collections.Generic;
using Common;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories.Entities
{
    internal class AssetPairSettingsEntity : TableEntity, IAssetPairSettingsEntity
    {
        public AssetPairSettingsEntity()
        {
            PartitionKey = "AssetSettings";
        }


        public string AssetPairId
        {
            get => RowKey;
            set => RowKey = value;
        }

        /// <inheritdoc />
        public AssetPairQuotesSourceTypeEnum QuotesSourceType { get; set; }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return new Dictionary<string, EntityProperty>
            {
                {nameof(PartitionKey), new EntityProperty(PartitionKey) },
                {nameof(RowKey), new EntityProperty(AssetPairId) },
                {nameof(QuotesSourceType), new EntityProperty(QuotesSourceType.ToString()) },
            };
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            foreach (var entityProperty in properties)
            {
                var propValue = entityProperty.Value.StringValue;
                switch (entityProperty.Key)
                {
                    case nameof(RowKey):
                        AssetPairId = propValue;
                        break;
                    case nameof(QuotesSourceType):
                        QuotesSourceType = propValue.ParseEnum<AssetPairQuotesSourceTypeEnum>();
                        break;
                }
            }
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