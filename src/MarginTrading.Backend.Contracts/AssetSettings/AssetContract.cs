using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.AssetSettings
{
    [PublicAPI]
    public class AssetContract : AssetInputContract
    {
        /// <summary>
        /// Instrument id
        /// </summary>
        public string Id { get; set; }
    }
}
