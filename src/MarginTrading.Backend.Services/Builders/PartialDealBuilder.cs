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
        private readonly decimal? _volume;

        /// <summary>
        /// Creates deal contract from order and position which was partially closed
        /// </summary>
        /// <param name="position">Position</param>
        /// <param name="order">Order</param>
        /// <param name="volume">Volume left to match, if not passed the order volume will be used</param>
        public PartialDealBuilder(Position position, Order order, decimal? volume = null) : base(position, order)
        {
            _volume = volume;
        }

        /// <inheritdoc />
        public override DealBuilder AddIdentity()
        {
            base.AddIdentity();

            Deal.DealId = AlphanumericIdentityGenerator.GenerateAlphanumericId();
            Deal.Volume = _volume ?? Math.Abs(Order.Volume);

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