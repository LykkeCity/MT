using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingOrderHistoryEntity : AzureTableEntity, IOrderHistory
    {
        public string Id { get; set; }
        public long Code { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public string Instrument { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }
        public decimal MatchedVolume { get; set; }
        public decimal MatchedCloseVolume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal Fpl { get; set; }
        public decimal PnL { get; set; }
        public decimal InterestRateSwap { get; set; }
        public decimal CommissionLot { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal QuoteRate { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal MarginInit { get; set; }
        public decimal MarginMaintenance { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public OrderDirection Type { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public OrderFillType FillType { get; set; }
        public OrderRejectReason RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public decimal SwapCommission { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent { get; set; }
        public decimal ClosePriceEquivalent { get; set; }
        public string OpenExternalOrderId { get; set; }
        public string OpenExternalProviderId { get; set; }
        public string CloseExternalOrderId { get; set; }
        public string CloseExternalProviderId { get; set; }
        public MatchingEngineMode MatchingEngineMode { get; set; }
        public string LegalEntity { get; set; }
        public DateTimeOffset UpdateTimestamp => Timestamp;
        public OrderUpdateType OrderUpdateType { get; set; }

        [ValueSerializer(typeof(JsonStorageValueSerializer))]
        public List<MatchedOrder> MatchedOrders { get; set; } = new List<MatchedOrder>();

        [ValueSerializer(typeof(JsonStorageValueSerializer))]
        public List<MatchedOrder> MatchedCloseOrders { get; set; } = new List<MatchedOrder>();

        public static string GeneratePartitionKey(string accountId)
        {
            return $"{clientId}_{accountId}";
        }

        public static MarginTradingOrderHistoryEntity Create(IOrderHistory src)
        {
            return new MarginTradingOrderHistoryEntity
            {
                PartitionKey = GeneratePartitionKey(src.AccountId),
                Id = src.Id,
                Code = src.Code,
                AccountId = src.AccountId,
                TradingConditionId = src.TradingConditionId,
                AccountAssetId = src.AccountAssetId,
                Instrument = src.Instrument,
                Type = src.Type,
                CreateDate = src.CreateDate,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Fpl = src.Fpl,
                PnL = src.PnL,
                InterestRateSwap = src.InterestRateSwap,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission,
                QuoteRate = src.QuoteRate,
                AssetAccuracy = src.AssetAccuracy,
                MarginInit = src.MarginInit,
                MarginMaintenance = src.MarginMaintenance,
                StartClosingDate = src.StartClosingDate,
                Status = src.Status,
                CloseReason = src.CloseReason,
                FillType = src.FillType,
                Volume = src.Volume,
                MatchedVolume = src.MatchedVolume,
                MatchedCloseVolume = src.MatchedCloseVolume,
                RejectReason = src.RejectReason,
                RejectReasonText = src.RejectReasonText,
                MatchedOrders = src.MatchedOrders,
                MatchedCloseOrders = src.MatchedCloseOrders,
                SwapCommission = src.SwapCommission,
                EquivalentAsset = src.EquivalentAsset,
                OpenPriceEquivalent = src.OpenPriceEquivalent,
                ClosePriceEquivalent = src.ClosePriceEquivalent,
                Comment = src.Comment,
                OrderUpdateType = src.OrderUpdateType,
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode,
                LegalEntity = src.LegalEntity,
            };
        }
    }

    public class MarginTradingOrdersHistoryRepository : IMarginTradingOrdersHistoryRepository
    {
        private readonly INoSQLTableStorage<MarginTradingOrderHistoryEntity> _tableStorage;

        public MarginTradingOrdersHistoryRepository(INoSQLTableStorage<MarginTradingOrderHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddAsync(IOrderHistory order)
        {
            var entity = MarginTradingOrderHistoryEntity.Create(order);
            // todo: write real date for non-trade events (and not creation)
            // ReSharper disable once RedundantArgumentDefaultValue
            return _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity,
                entity.CloseDate ?? entity.OpenDate ?? entity.CreateDate, RowKeyDateTimeFormat.Iso);
        }

        public async Task<IReadOnlyList<IOrderHistory>> GetHistoryAsync(string clientId, string[] accountIds,
            DateTime? from, DateTime? to)
        {
            return (await _tableStorage.WhereAsync(accountIds.Select(a => clientId + '_' + a),
                    from ?? DateTime.MinValue, to?.Date.AddDays(1) ?? DateTime.MaxValue, ToIntervalOption.IncludeTo))
                .OrderByDescending(entity => entity.CloseDate ?? entity.OpenDate ?? entity.CreateDate).ToList();
        }

        public async Task<IEnumerable<IOrderHistory>> GetHistoryAsync()
        {
            var entities = (await _tableStorage.GetDataAsync()).OrderByDescending(item => item.Timestamp);

            return entities;
        }
    }
}