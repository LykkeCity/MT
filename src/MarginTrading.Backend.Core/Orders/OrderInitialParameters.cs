// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Backend.Core.Orders
{
    public class OrderInitialParameters
    {
        public string Id { get; set; }
        
        public long Code { get; set; }
        
        public DateTime Now { get; set; }
        
        public decimal EquivalentPrice { get; set; }
        
        public decimal FxPrice { get; set; }
        
        public string FxAssetPairId { get; set; }
        
        public FxToAssetPairDirection FxToAssetPairDirection { get; set; }
    }
}