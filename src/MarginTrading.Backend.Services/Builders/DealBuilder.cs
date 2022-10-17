// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.PnL;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.Builders
{
    /// <summary>
    /// Creates deal contract from order and position which was fully closed
    /// </summary>
    internal class DealBuilder
    {
        protected readonly DealContract Deal;
        protected readonly Position Position;
        protected readonly Order Order;
        
        public DealBuilder(Position position, Order order)
        {
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Order = order ?? throw new ArgumentNullException(nameof(order));
            Deal = new DealContract();
        }
        
        /// <summary>
        /// Adds id and basic deal properties
        /// </summary>
        /// <returns></returns>
        public virtual DealBuilder AddIdentity()
        {
            Deal.DealId = Position.Id;
            Deal.PositionId = Position.Id;
            Deal.Volume = Math.Abs(Position.Volume);
            Deal.Created = Order.Created;
            Deal.Originator = Order.Originator.ToType<OriginatorTypeContract>();
            Deal.AdditionalInfo = Order.AdditionalInfo;
            
            return this;
        }

        /// <summary>
        /// Adds properties related to position opening
        /// </summary>
        /// <returns></returns>
        public DealBuilder AddOpenPart()
        {
            Deal.OpenTradeId = Position.OpenTradeId;
            Deal.OpenOrderType = Position.OpenOrderType.ToType<OrderTypeContract>();
            Deal.OpenOrderVolume = Position.OpenOrderVolume;
            Deal.OpenOrderExpectedPrice = Position.ExpectedOpenPrice;
            Deal.OpenPrice = Position.OpenPrice;
            Deal.OpenFxPrice = Position.OpenFxPrice;

            return this;
        }

        /// <summary>
        /// Adds properties related to position closing
        /// </summary>
        /// <returns></returns>
        public DealBuilder AddClosePart()
        {
            Deal.CloseTradeId = Order.Id;
            Deal.CloseOrderType = Order.OrderType.ToType<OrderTypeContract>();
            Deal.CloseOrderVolume = Order.Volume;
            Deal.CloseOrderExpectedPrice = Order.Price;
            Deal.ClosePrice = Order.ExecutionPrice.Value;
            Deal.CloseFxPrice = Order.FxRate;    
            
            return this;
        }

        /// <summary>
        /// Adds PnL information
        /// </summary>
        /// <returns></returns>
        public DealBuilder AddPnl()
        {
            PnlBase pnl = Position.Volume > 0 ?
                new PnlLong(Position.OpenPrice, Order.ExecutionPrice.Value, Deal.Volume, Order.FxRate) :
                new PnlShort(Position.OpenPrice, Order.ExecutionPrice.Value, Deal.Volume, Order.FxRate);
            Deal.Fpl = pnl.Value.WithDefaultAccuracy();

            return this;
        }

        /// <summary>
        /// Adds charged PnL information
        /// </summary>
        /// <returns></returns>
        public virtual DealBuilder AddChargedPnl()
        {
            var chargedPnL = Position.ChargedPnL;
            Deal.PnlOfTheLastDay = Deal.Fpl - chargedPnL.WithDefaultAccuracy();

            return this;
        }

        public DealContract Build() => Deal.ShallowCopy();
    }
}