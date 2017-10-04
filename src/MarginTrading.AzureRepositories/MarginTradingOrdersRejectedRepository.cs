using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using MarginTrading.Core;
using MarginTrading.Core.MatchedOrders;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingOrderRejectedEntity : TableEntity, IOrderHistory
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public string Instrument { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal OpenCrossPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal CloseCrossPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal MatchedVolume { get; set; }
        public decimal MatchedCloseVolume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal Fpl { get; set; }
        public decimal PnL { get; set; }
        public decimal InterestRateSwap { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal QuoteRate { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal MarginInit { get; set; }
        public decimal MarginMaintenance { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public string Type { get; set; }
        OrderDirection IOrderHistory.Type => Type.ParseEnum(OrderDirection.Buy);
        public string Status { get; set; }
        OrderStatus IOrderHistory.Status => Status.ParseEnum(OrderStatus.Closed);
        public string CloseReason { get; set; }
        OrderCloseReason IOrderHistory.CloseReason => CloseReason.ParseEnum(OrderCloseReason.Close);
        public string FillType { get; set; }
        OrderFillType IOrderHistory.FillType => FillType.ParseEnum(OrderFillType.FillOrKill);
        public string RejectReason { get; set; }
        OrderRejectReason IOrderHistory.RejectReason => RejectReason.ParseEnum(OrderRejectReason.None);
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public List<MatchedOrder> MatchedOrders { get; set; } = new List<MatchedOrder>();
        public List<MatchedOrder> MatchedCloseOrders { get; set; } = new List<MatchedOrder>();
        public decimal SwapCommission { get; set; }

        public string Orders { get; set; }
        public string ClosedOrders { get; set; }

        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static MarginTradingOrderRejectedEntity Create(IOrderHistory src)
        {
            return new MarginTradingOrderRejectedEntity
            {
                PartitionKey = GeneratePartitionKey(src.ClientId),
                RowKey = GenerateRowKey(src.Id),
                Id = src.Id,
                ClientId = src.ClientId,
                AccountId = src.AccountId,
                TradingConditionId = src.TradingConditionId,
                AccountAssetId = src.AccountAssetId,
                Instrument = src.Instrument,
                Type = src.Type.ToString(),
                CreateDate = src.CreateDate,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                OpenCrossPrice = src.OpenCrossPrice,
                ClosePrice = src.ClosePrice,
                CloseCrossPrice = src.CloseCrossPrice,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Fpl = src.Fpl,
                PnL = src.PnL,
                InterestRateSwap = src.InterestRateSwap,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission,
                QuoteRate = src.QuoteRate,
                AssetAccuracy = src.AssetAccuracy,
                MarginInit = src.MarginInit,
                MarginMaintenance = src.MarginMaintenance,
                StartClosingDate = src.StartClosingDate,
                Status = src.Status.ToString(),
                CloseReason = src.CloseReason.ToString(),
                FillType = src.FillType.ToString(),
                Volume = src.Volume,
                MatchedVolume = src.MatchedVolume,
                MatchedCloseVolume = src.MatchedCloseVolume,
                RejectReason = src.RejectReason.ToString(),
                RejectReasonText = src.RejectReasonText,
                Orders = src.MatchedOrders.SerializeArrayForTableStorage(),
                ClosedOrders = src.MatchedCloseOrders.SerializeArrayForTableStorage(),
                SwapCommission = src.SwapCommission,
                Comment = src.Comment
            };
        }
    }

    public class MarginTradingOrdersRejectedRepository : IMarginTradingOrdersRejectedRepository
    {
        private readonly INoSQLTableStorage<MarginTradingOrderRejectedEntity> _tableStorage;

        public MarginTradingOrdersRejectedRepository(INoSQLTableStorage<MarginTradingOrderRejectedEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }
        public async Task AddAsync(IOrderHistory order)
        {
            var entity = MarginTradingOrderRejectedEntity.Create(order);
            await _tableStorage.InsertOrReplaceAsync(entity);
        }

        public async Task<IEnumerable<IOrderHistory>> GetHisotryAsync(string[] accountIds, DateTime from, DateTime to)
        {
            var entities = (await _tableStorage.GetDataAsync(entity => accountIds.Contains(entity.AccountId) && entity.CloseDate >= from && entity.CloseDate <= to))
                .OrderByDescending(item => item.Timestamp);

            foreach (var entity in entities.Where(item => item.Id == null))
            {
                entity.Id = entity.RowKey;
            }

            return entities;
        }
    }
}
