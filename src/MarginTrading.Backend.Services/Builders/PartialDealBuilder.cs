// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Builders
{
    /// <summary>
    /// Creates deal contract from order and position which was partially closed
    /// </summary>
    internal sealed class PartialDealBuilder : DealBuilder
    {
        public PartialDealBuilder(Position position, Order order) : base(position, order)
        {
        }

        /// <inheritdoc />
        public override DealBuilder AddIdentity()
        {
            base.AddIdentity();
            
            Deal.DealId = AlphanumericIdentityGenerator.GenerateAlphanumericId();
            Deal.Volume = Math.Abs(Order.Volume);
        
            return this;
        }

        /// <inheritdoc />
        public override DealBuilder AddChargedPnl()
        {
            var chargedPnL = Deal.Volume / Math.Abs(Position.Volume) * Position.ChargedPnL;
            Deal.PnlOfTheLastDay = Deal.Fpl - chargedPnL.WithDefaultAccuracy();
            
            return this;
        }
    }
}