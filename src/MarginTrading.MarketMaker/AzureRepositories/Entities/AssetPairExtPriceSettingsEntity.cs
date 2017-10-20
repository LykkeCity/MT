using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace MarginTrading.MarketMaker.AzureRepositories.Entities
{
    internal class AssetPairExtPriceSettingsEntity : TableEntity, IAssetPairSettingsEntity
    {
        public AssetPairExtPriceSettingsEntity()
        {
            PartitionKey = "AssetPairExtPriceSettings";
        }

        public string AssetPairId
        {
            get => RowKey;
            set => RowKey = value;
        }

        public double AskMarkup { get; set; }
        public double BidMarkup { get; set; }
        public string PresetDefaultExchange { get; set; }
        public double OutlierThreshold { get; set; }
        public RepeatedOutliersParams RepeatedOutliers { get; set; } = new RepeatedOutliersParams();

        [EditorBrowsable(EditorBrowsableState.Never), CanBeNull, JsonIgnore]
        public string StepsStr;

        [CanBeNull]
        private ImmutableDictionary<OrderbookGeneratorStepEnum, bool> _stepsCache;

        public ImmutableDictionary<OrderbookGeneratorStepEnum, bool> Steps
        {
            get => _stepsCache ?? (_stepsCache = StepsStr == null
                       ? ImmutableDictionary<OrderbookGeneratorStepEnum, bool>.Empty
                       : JsonConvert.DeserializeObject<ImmutableDictionary<OrderbookGeneratorStepEnum, bool>>(StepsStr));
            
            set => StepsStr = JsonConvert.SerializeObject(_stepsCache = value);
        }

        /// <inheritdoc />
        public AssetPairQuotesSourceTypeEnum QuotesSourceType { get; set; }

        public static string GeneratePartitionKey()
        {
            return "AssetPairExtPriceSettings";
        }

        public static string GenerateRowKey(string assetName)
        {
            return assetName;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties,
            OperationContext operationContext)
        {
            ShallowCopyHelper<AssetPairExtPriceSettingsEntity>.Copy(
                ConvertBack<AssetPairExtPriceSettingsEntity>(properties, operationContext), this);
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return Flatten(this, operationContext);
        }

        public class RepeatedOutliersParams
        {
            public int MaxSequenceLength { get; set; }
            public TimeSpan MaxSequenceAge { get; set; }
            public double MaxAvg { get; set; }
            public TimeSpan MaxAvgAge { get; set; }
        }
    }
}