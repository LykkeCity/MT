using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.AssetSettings
{
    [PublicAPI]
    public class AssetInputContract
    {
        /// <summary>
        /// Instrument display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Instrument accuracy in decimal digits count
        /// </summary>
        public int Accuracy { get; set; }
    }
}
