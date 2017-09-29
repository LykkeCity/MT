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
using Newtonsoft.Json;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingOrderHistoryEntity : TableEntity, IOrderHistory
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
        public double? ExpectedOpenPrice { get; set; }
        public double OpenPrice { get; set; }
        public double OpenCrossPrice { get; set; }
        public double ClosePrice { get; set; }
        public double CloseCrossPrice { get; set; }
        public double Volume { get; set; }
        public double MatchedVolume { get; set; }
        public double MatchedCloseVolume { get; set; }
        public double? TakeProfit { get; set; }
        public double? StopLoss { get; set; }
        public double Fpl { get; set; }
        public double PnL { get; set; }
        public double InterestRateSwap { get; set; }
        public double OpenCommission { get; set; }
        public double CloseCommission { get; set; }
        public double QuoteRate { get; set; }
        public int AssetAccuracy { get; set; }
        public double MarginInit { get; set; }
        public double MarginMaintenance { get; set; }
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
        public double SwapCommission { get; set; }

        public string Orders { get; set; }
        public string ClosedOrders { get; set; }

        public static string GeneratePartitionKey(string clientId, string accountIds)
        {
            return $"{clientId}_{accountIds}";
        }

        public static MarginTradingOrderHistoryEntity Create(IOrderHistory src)
        {
            return new MarginTradingOrderHistoryEntity
            {
                PartitionKey = GeneratePartitionKey(src.ClientId, src.AccountId),
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


        public static IOrder Restore(IOrderHistory historyOrder)
        {
            Order order = new Order();

            if (historyOrder == null)
                return order;

            order.Id = historyOrder.Id;
            order.ClientId = historyOrder.ClientId;
            order.AccountId = historyOrder.AccountId;
            order.TradingConditionId = historyOrder.TradingConditionId;
            order.AccountAssetId = historyOrder.AccountAssetId;
            order.Instrument = historyOrder.Instrument;
            order.CreateDate = historyOrder.CreateDate;
            order.OpenDate = historyOrder.OpenDate;
            order.CloseDate = historyOrder.CloseDate;
            order.ExpectedOpenPrice = historyOrder.ExpectedOpenPrice;
            order.OpenPrice = historyOrder.OpenPrice;
            order.TakeProfit = historyOrder.TakeProfit;
            order.StopLoss = historyOrder.StopLoss;
            order.OpenCommission = historyOrder.OpenCommission;
            order.CloseCommission = historyOrder.CloseCommission;
            order.QuoteRate = historyOrder.QuoteRate;
            order.AssetAccuracy = historyOrder.AssetAccuracy;
            order.StartClosingDate = historyOrder.StartClosingDate;
            order.Volume = historyOrder.Volume;
            order.SwapCommission = historyOrder.SwapCommission;
            order.Comment = historyOrder.Comment;
            order.ClosePrice = historyOrder.ClosePrice;
            order.RejectReasonText = historyOrder.RejectReasonText;

            if (historyOrder is MarginTradingOrderHistoryEntity)
            {
                OrderStatus status;
                if (Enum.TryParse(((MarginTradingOrderHistoryEntity)historyOrder).Status, out status))
                {
                    order.Status = status;
                }
                OrderCloseReason closeReason;
                if (Enum.TryParse(((MarginTradingOrderHistoryEntity)historyOrder).CloseReason, out closeReason))
                {
                    order.CloseReason = closeReason;
                }
                OrderFillType fillType;
                if (Enum.TryParse(((MarginTradingOrderHistoryEntity)historyOrder).FillType, out fillType))
                {
                    order.FillType = fillType;
                }
                OrderRejectReason rejectReason;
                if (Enum.TryParse(((MarginTradingOrderHistoryEntity)historyOrder).RejectReason, out rejectReason))
                {
                    order.RejectReason = rejectReason;
                }

                order.MatchedOrders = new MatchedOrderCollection(
                    ((MarginTradingOrderHistoryEntity) historyOrder).Orders != null
                        ? JsonConvert.DeserializeObject<List<MatchedOrder>>(
                            ((MarginTradingOrderHistoryEntity) historyOrder).Orders)
                        : new List<MatchedOrder>());
                order.MatchedCloseOrders = new MatchedOrderCollection(
                    ((MarginTradingOrderHistoryEntity) historyOrder).ClosedOrders != null
                        ? JsonConvert.DeserializeObject<List<MatchedOrder>>(
                            ((MarginTradingOrderHistoryEntity) historyOrder).ClosedOrders)
                        : new List<MatchedOrder>());
            }

            return order;
        }
    } 

    public class MarginTradingOrdersHistoryRepository : IMarginTradingOrdersHistoryRepository
    {
        private readonly INoSQLTableStorage<MarginTradingOrderHistoryEntity> _tableStorage;

        public MarginTradingOrdersHistoryRepository(INoSQLTableStorage<MarginTradingOrderHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddAsync(IOrderHistory order)
        {
            var entity = MarginTradingOrderHistoryEntity.Create(order);
            await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, DateTime.UtcNow);
        }

        public async Task<IEnumerable<IOrderHistory>> GetHistoryAsync(string clientId, string[] accountIds, DateTime? from, DateTime? to)
        {
            var partitionKeys = new List<string>();

            foreach (var accountId in accountIds)
                partitionKeys.Add(MarginTradingOrderHistoryEntity.GeneratePartitionKey(clientId, accountId));

            var entities = (await _tableStorage.GetDataAsync(partitionKeys, int.MaxValue, 
                entity => ((entity.CloseDate ?? entity.OpenDate) >= from || from == null) && ((entity.CloseDate ?? entity.OpenDate) <= to || to == null)))
                .OrderByDescending(item => item.Timestamp);

            return entities;
        }

        public async Task<IEnumerable<IOrderHistory>> GetHistoryAsync()
        {
            var entities = (await _tableStorage.GetDataAsync()).OrderByDescending(item => item.Timestamp);

            return entities;
        }
    }
}
