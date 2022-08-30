// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public class SpecialLiquidationOperationData : OperationDataBase<SpecialLiquidationOperationState>
    {
        public string Instrument { get; set; }
        // todo: make setter private
        public List<string> PositionIds { get; set; }
        // todo: make setter private
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public string ExternalProviderId { get; set; }
        [CanBeNull]
        public string AccountId { get; set; }
        [CanBeNull]
        public string CausationOperationId { get; set; }
        public string AdditionalInfo { get; set; }
        public OriginatorType OriginatorType { get; set; }
        // todo: make setter private
        public int RequestNumber { get; set; }
        public bool RequestedFromCorporateActions { get; set; }

        /// <summary>
        /// Updates the volume and positions list with the actual state of orders/positions cache
        /// </summary>
        /// <param name="actualPositionsGetter">The source of actual positions</param>
        /// <returns>True if volume has changed, false otherwise</returns>
        public bool Sync(Func<IEnumerable<Position>> actualPositionsGetter)
        {
            var actualPositionsList = actualPositionsGetter()
                .Where(x => PositionIds.Contains(x.Id) && (string.IsNullOrEmpty(AccountId) || x.AccountId == AccountId))
                .ToList();
            
            PositionIds = actualPositionsList.Select(p => p.Id).ToList();
            
            var newVolume = -actualPositionsList.Sum(p => p.Volume);

            if (newVolume != 0 && newVolume != Volume)
            {
                Volume = newVolume;
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Increments requests counter
        /// </summary>
        /// <returns>The new value of the counter</returns>
        public int NextRequestNumber()
        {
            return ++RequestNumber;
        }
    }
}