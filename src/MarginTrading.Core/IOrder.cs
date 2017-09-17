using System;
using System.Linq;
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
        double? ExpectedOpenPrice { get; }
        double OpenPrice { get; }
        double ClosePrice { get; }
        double? TakeProfit { get; }
        double? StopLoss { get; }
        double OpenCommission { get; }
        double CloseCommission { get; }
        double CommissionLot { get; }
        double QuoteRate { get; }
        int AssetAccuracy { get; }
        DateTime? StartClosingDate { get; }
        OrderStatus Status { get; }
        OrderCloseReason CloseReason { get; }
        OrderFillType FillType { get; }
        OrderRejectReason RejectReason { get; }
        string RejectReasonText { get; }
        string Comment { get; }
        MatchedOrderCollection MatchedCloseOrders { get; }
        double SwapCommission { get; }
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
        public double? ExpectedOpenPrice { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public double QuoteRate { get; set; }
        public int AssetAccuracy { get; set; }
        public double Volume { get; set; }
        public double? TakeProfit { get; set; }
        public double? StopLoss { get; set; }
        public double OpenCommission { get; set; }
        public double CloseCommission { get; set; }
        public double CommissionLot { get; set; }
        public double SwapCommission { get; set; }
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
        public static bool IsSuitablePriceForPendingOrder(this IOrder order, double price)
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
            double volume = Math.Abs(order.Volume);
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
                            Math.Round(order.GetTotalFpl(), order.AssetAccuracy).ToString($"F{order.AssetAccuracy}"));
                    break;
                case OrderStatus.Rejected:
                    break;
            }

            return message;
        }

        public static double GetTotalFpl(this IOrder order)
        {
            return GetTotalFpl(order, order.GetSwaps());
        }

        public static double GetTotalFpl(this IOrder order, double swaps)
        {
            return order.GetFpl() - order.GetOpenCommission() - order.GetCloseCommission() - order.SwapCommission - swaps;
        }

        public static double GetTotalFpl(this IOrder order, int accuracy)
        {
            return Math.Round(order.GetTotalFpl(), accuracy);
        }

        public static double GetMatchedVolume(this IOrder order)
        {
            return order.MatchedOrders.SummaryVolume;
        }

        public static double GetMatchedCloseVolume(this IOrder order)
        {
            return order.MatchedCloseOrders.SummaryVolume;
        }

        public static double GetRemainingCloseVolume(this IOrder order)
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

        public static double GetFpl(this IOrder order)
        {
            return order.GetFplData().Fpl;
        }

        public static double GetQuoteRate(this IOrder order)
        {
            return order.GetFplData().QuoteRate;
        }

        public static double GetMarginMaintenance(this IOrder order)
        {
            return order.GetFplData().MarginMaintenance;
        }

        public static double GetMarginInit(this IOrder order)
        {
            return order.GetFplData().MarginInit;
        }

        public static double GetOpenCrossPrice(this IOrder order)
        {
            return order.GetFplData().OpenCrossPrice;
        }

        public static double GetCloseCrossPrice(this IOrder order)
        {
            return order.GetFplData().CloseCrossPrice;
        }

        public static void UpdateClosePrice(this IOrder order, double closePrice)
        {
            var orderInstance = order as Order;

            if (orderInstance != null)
            {
                orderInstance.ClosePrice = closePrice;
                var account = MtServiceLocator.AccountsCacheService.Get(order.ClientId, order.AccountId);
                account.CacheNeedsToBeUpdated();
            }
        }

        public static double GetSwaps(this IOrder order)
        {
            return MtServiceLocator.SwapCommissionService.GetSwaps(order);
        }

        public static double GetOpenCommission(this IOrder order)
        {
            return order.CommissionLot == 0 ? 0 : order.Volume / order.CommissionLot * order.OpenCommission;
        }

        public static double GetCloseCommission(this IOrder order)
        {
            return order.CommissionLot == 0 ? 0 : order.GetMatchedCloseVolume() / order.CommissionLot * order.CloseCommission;
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
