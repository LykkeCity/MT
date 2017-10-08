using System;
using System.Linq;
using MarginTrading.Core.MatchedOrders;
using MarginTrading.Core.Messages;

namespace MarginTrading.Core
{
    public interface IOrder : IBaseOrder
    {
        string ClientId { get; }
        string AccountId { get; }
        string TradingConditionId { get; }
        string AccountAssetId { get; }
        string OpenOrderbookId { get; }
        string CloseOrderbookId { get; }
        DateTime? OpenDate { get; }
        DateTime? CloseDate { get; }
        decimal? ExpectedOpenPrice { get; }
        decimal OpenPrice { get; }
        decimal ClosePrice { get; }
        decimal? TakeProfit { get; }
        decimal? StopLoss { get; }
        decimal OpenCommission { get; }
        decimal CloseCommission { get; }
        decimal CommissionLot { get; }
        decimal QuoteRate { get; }
        int AssetAccuracy { get; }
        DateTime? StartClosingDate { get; }
        OrderStatus Status { get; }
        OrderCloseReason CloseReason { get; }
        OrderFillType FillType { get; }
        OrderRejectReason RejectReason { get; }
        string RejectReasonText { get; }
        string Comment { get; }
        MatchedOrderCollection MatchedCloseOrders { get; }
        decimal SwapCommission { get; }
    }

    public class Order : IOrder
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public string OpenOrderbookId { get; set; }
        public string CloseOrderbookId { get; set; }
        public string Instrument { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal QuoteRate { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal CommissionLot { get; set; }
        public decimal SwapCommission { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public OrderFillType FillType { get; set; }
        public OrderRejectReason RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public MatchedOrderCollection MatchedOrders { get; set; } = new MatchedOrderCollection();
        public MatchedOrderCollection MatchedCloseOrders { get; set; } = new MatchedOrderCollection();

        public FplData FplData { get; set; } = new FplData();
    }

    public enum OrderFillType
    {
        FillOrKill,
        PartialFill
    }

    public enum OrderDirection
    {
        Buy,
        Sell
    }

    public enum OrderStatus
    {
        WaitingForExecution,
        Active,
        Closed,
        Rejected,
        Closing
    }

    public enum OrderCloseReason
    {
        None,
        Close,
        StopLoss,
        TakeProfit,
        StopOut,
        Canceled
    }

    public enum OrderRejectReason
    {
        None,
        NoLiquidity,
        NotEnoughBalance,
        LeadToStopOut,
        AccountInvalidState,
        InvalidExpectedOpenPrice,
        InvalidVolume,
        InvalidTakeProfit,
        InvalidStoploss,
        InvalidInstrument,
        InvalidAccount,
        TradingConditionError,
        TechnicalError
    }

    public enum OrderUpdateType
    {
        Open,
        TakeProfitTrigger,
        StopLossTrigger,
        ExpectedOpenPriceTrigger,
        Update,
        ClosePending,
        ClosePosition
    }

    public static class OrderExt
    {
        public static bool IsSuitablePriceForPendingOrder(this IOrder order, decimal price)
        {
            return order.ExpectedOpenPrice.HasValue && (order.GetOrderType() == OrderDirection.Buy && price <= order.ExpectedOpenPrice
                || order.GetOrderType() == OrderDirection.Sell && price >= order.ExpectedOpenPrice);
        }

        public static bool IsStopLoss(this IOrder order)
        {
            return order.GetOrderType() == OrderDirection.Buy
                ? order.StopLoss.HasValue && order.StopLoss.Value > 0 && order.ClosePrice <= order.StopLoss
                : order.StopLoss.HasValue && order.StopLoss.Value > 0 && order.ClosePrice >= order.StopLoss;
        }

        public static bool IsTakeProfit(this IOrder order)
        {
            return order.GetOrderType() == OrderDirection.Buy
                ? order.TakeProfit.HasValue && order.TakeProfit > 0 && order.ClosePrice >= order.TakeProfit
                : order.TakeProfit.HasValue && order.TakeProfit > 0 && order.ClosePrice <= order.TakeProfit;
        }

        public static string GetPushMessage(this IOrder order)
        {
            string message = string.Empty;
            decimal volume = Math.Abs(order.Volume);
            string type = order.GetOrderType() == OrderDirection.Buy ? "Long" : "Short";

            switch (order.Status)
            {
                case OrderStatus.WaitingForExecution:
                    message = string.Format(MtMessages.Notifications_PendingOrderPlaced, type, order.Instrument, volume, Math.Round(order.ExpectedOpenPrice ?? 0, order.AssetAccuracy));
                    break;
                case OrderStatus.Active:
                    message = order.ExpectedOpenPrice.HasValue
                        ? string.Format(MtMessages.Notifications_PendingOrderTriggered, order.GetOrderType() == OrderDirection.Buy ? "Long" : "Short", order.Instrument, volume,
                            Math.Round(order.OpenPrice, order.AssetAccuracy))
                        : string.Format(MtMessages.Notifications_OrderPlaced, type, order.Instrument, volume,
                            Math.Round(order.OpenPrice, order.AssetAccuracy));
                    break;
                case OrderStatus.Closed:
                    string reason = string.Empty;

                    switch (order.CloseReason)
                    {
                        case OrderCloseReason.StopLoss:
                            reason = MtMessages.Notifications_WithStopLossPhrase;
                            break;
                        case OrderCloseReason.TakeProfit:
                            reason = MtMessages.Notifications_WithTakeProfitPhrase;
                            break;
                    }

                    message = order.ExpectedOpenPrice.HasValue && order.CloseReason == OrderCloseReason.Canceled
                        ? string.Format(MtMessages.Notifications_PendingOrderCanceled, type, order.Instrument, volume)
                        : string.Format(MtMessages.Notifications_OrderClosed, type, order.Instrument, volume, reason,
                            order.GetTotalFpl().ToString($"F{MarginTradingHelpers.DefaultAssetAccuracy}"));
                    break;
                case OrderStatus.Rejected:
                    break;
            }

            return message;
        }

        public static decimal GetTotalFpl(this IOrder order, decimal swaps)
        {
            return order.GetFpl() - order.GetOpenCommission() - order.GetCloseCommission() - swaps;
        }

        public static decimal GetTotalFpl(this IOrder order)
        {
            return Math.Round(GetTotalFpl(order, order.GetSwaps()), MarginTradingHelpers.DefaultAssetAccuracy);
        }

        public static decimal GetMatchedVolume(this IOrder order)
        {
            return order.MatchedOrders.SummaryVolume;
        }

        public static decimal GetMatchedCloseVolume(this IOrder order)
        {
            return order.MatchedCloseOrders.SummaryVolume;
        }

        public static decimal GetRemainingCloseVolume(this IOrder order)
        {
            return order.GetMatchedVolume() - order.GetMatchedCloseVolume();
        }

        public static bool GetIsCloseFullfilled(this IOrder order)
        {
            return Math.Round(order.GetRemainingCloseVolume(), MarginTradingHelpers.VolumeAccuracy) == 0;
        }

        private static FplData GetFplData(this IOrder order)
        {
            var orderInstance = order as Order;

            if (orderInstance != null)
            {
                if (orderInstance.FplData.OpenPrice != order.OpenPrice ||
                    orderInstance.FplData.ClosePrice != order.ClosePrice)
                {
                    MtServiceLocator.FplService.UpdateOrderFpl(orderInstance, orderInstance.FplData);
                }

                return orderInstance.FplData;
            }

            var fplData = new FplData();
            MtServiceLocator.FplService.UpdateOrderFpl(order, fplData);

            return fplData;
        }

        public static decimal GetFpl(this IOrder order)
        {
            return order.GetFplData().Fpl;
        }

        public static decimal GetQuoteRate(this IOrder order)
        {
            return order.GetFplData().QuoteRate;
        }

        public static decimal GetMarginMaintenance(this IOrder order)
        {
            return order.GetFplData().MarginMaintenance;
        }

        public static decimal GetMarginInit(this IOrder order)
        {
            return order.GetFplData().MarginInit;
        }

        public static decimal GetOpenCrossPrice(this IOrder order)
        {
            return order.GetFplData().OpenCrossPrice;
        }

        public static decimal GetCloseCrossPrice(this IOrder order)
        {
            return order.GetFplData().CloseCrossPrice;
        }

        public static void UpdateClosePrice(this IOrder order, decimal closePrice)
        {
            var orderInstance = order as Order;

            if (orderInstance != null)
            {
                orderInstance.ClosePrice = closePrice;
                var account = MtServiceLocator.AccountsCacheService.Get(order.ClientId, order.AccountId);
                account.CacheNeedsToBeUpdated();
            }
        }

        public static decimal GetSwaps(this IOrder order)
        {
            return MtServiceLocator.SwapCommissionService.GetSwaps(order);
        }

        public static decimal GetOpenCommission(this IOrder order)
        {
            return order.CommissionLot == 0 ? 0 : Math.Abs(order.Volume) / order.CommissionLot * order.OpenCommission;
        }

        public static decimal GetCloseCommission(this IOrder order)
        {
            return order.CommissionLot == 0 ? 0 : Math.Abs(order.GetMatchedCloseVolume()) / order.CommissionLot * order.CloseCommission;
        }
    }

    public static class OrderTypeExtension
    {
        public static OrderDirection GetOrderTypeToMatchInOrderBook(this OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
        }
    }
}
