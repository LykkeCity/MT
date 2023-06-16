// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core
{
    public class FplData
    {
        public decimal RawFpl { get; set; }
        [Obsolete]
        public decimal Fpl { get; set; }
        public decimal MarginRate { get; set; }
        public decimal MarginInit { get; set; }
        public decimal MarginMaintenance { get; set; }
        public int AccountBaseAssetAccuracy { get; set; }
        
        /// <summary>
        /// Margin used for open of position
        /// </summary>
        public decimal InitialMargin { get; set; }
        
        public int CalculatedHash { get; set; }
        public int ActualHash { get; set; }
        public string LogInfo { get; set; }
    }
}
