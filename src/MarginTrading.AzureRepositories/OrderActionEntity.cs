using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.AzureRepositories
{
    public class OrderActionEntity : TableEntity
    {
        public string LykkeOrderId { get; set; }
        public string OrderType { get; set; }
        public string TraderLykkeId { get; set; }
        public string CoreSide { get; set; }
        public string CoreSymbol { get; set; }
        public string TraderExternalSymbol { get; set; }
        public double? TraderRequestedPrice { get; set; }
        public string TimeForceCondition { get; set; }
        public string TraderAction { get; set; }
        public double? ExecutionDuration { get; set; }
        public string GeneratedFrom { get; set; }
        public string OrderId { get; set; }

        private static string GeneratePartitionKey(IOrderAction order)
        {
            return $"{order.TraderLykkeId}_{order.CoreSymbol}";
        }

        private static string GenerateRowKey(IOrderAction order)
        {
            return $"{order.LykkeOrderId}_{order.TraderAction}";
        }

        public static IOrderAction Restore(OrderActionEntity entity)
        {
            if (entity == null)
                return null;

            var orderAction = new OrderAction();

            orderAction.LykkeOrderId = entity.LykkeOrderId;
            orderAction.TraderLykkeId = entity.TraderLykkeId;
            orderAction.CoreSymbol = entity.CoreSymbol;
            orderAction.TraderExternalSymbol = entity.TraderExternalSymbol;
            orderAction.TakerRequestedPrice = entity.TraderRequestedPrice;
            orderAction.ExecutionDuration = entity.ExecutionDuration;
            orderAction.OrderId = entity.OrderId;

            OrderDirection coreSide;
            if (Enum.TryParse(entity.CoreSide, out coreSide))
            {
                orderAction.CoreSide = coreSide;
            }

            TimeForceCondition timeForceCondition;
            if (Enum.TryParse(entity.TimeForceCondition, out timeForceCondition))
            {
                orderAction.TimeForceCondition = timeForceCondition;
            }

            TraderAction traderAction;
            if (Enum.TryParse(entity.TraderAction, out traderAction))
            {
                orderAction.TraderAction = traderAction;
            }

            return orderAction; 
        }

        public static OrderActionEntity Create(IOrderAction order)
        {
            return new OrderActionEntity
            {
                PartitionKey = GeneratePartitionKey(order),
                RowKey = GenerateRowKey(order),
                LykkeOrderId = order.LykkeOrderId,
                OrderId = order.OrderId,
                TraderLykkeId = order.TraderLykkeId,
                CoreSide = order.CoreSide.ToString(),
                CoreSymbol = order.CoreSymbol,
                TraderExternalSymbol = order.TraderExternalSymbol,
                TraderRequestedPrice = order.TakerRequestedPrice,
                TimeForceCondition = order.TimeForceCondition.ToString(),
                TraderAction = order.TraderAction.ToString(),
                ExecutionDuration = order.ExecutionDuration,
                GeneratedFrom = order.IsLive ? "Live" : "History"
            };
        }
    }
}
