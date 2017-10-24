using System;
using System.Collections.Generic;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.MarketMaker.AzureRepositories.Entities
{
    internal class ExchangeExtPriceSettingsEntity : TableEntity
    {
        public string AssetPairId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string Exchange
        {
            get => RowKey;
            set => RowKey = value;
        }

        public TimeSpan OrderbookOutdatingThreshold { get; set; }
        public DisabledSettings Disabled { get; set; } = new DisabledSettings();
        public HedgingSettings Hedging { get; set; } = new HedgingSettings();
        public OrderGenerationSettings OrderGeneration { get; set; } = new OrderGenerationSettings();

        public static string GeneratePartitionKey(string assetPairId)
        {
            return assetPairId;
        }

        public static string GenerateRowKey(string exchange)
        {
            return exchange;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties,
            OperationContext operationContext)
        {
            ShallowCopyHelper<ExchangeExtPriceSettingsEntity>.Copy(
                ConvertBack<ExchangeExtPriceSettingsEntity>(properties, operationContext), this);
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return Flatten(this, operationContext);
        }

        public class DisabledSettings
        {
            public bool IsTemporarilyDisabled { get; set; }
            public string Reason { get; set; }
        }

        public class HedgingSettings
        {
            public double DefaultPreference { get; set; }
            public bool IsTemporarilyUnavailable { get; set; }
        }

        public class OrderGenerationSettings
        {
            public double VolumeMultiplier { get; set; }
            public TimeSpan OrderRenewalDelay { get; set; }
        }
    }
}