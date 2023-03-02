// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Settings
{
    /// <summary>
    /// Obsolete feature flag
    /// </summary>
    public class ObsoleteFeature
    {
        public bool IsEnabled { get; set; }

        public ObsoleteFeature()
        {
            IsEnabled = false;
        }

        public static ObsoleteFeature Default => new ObsoleteFeature();
    }
}