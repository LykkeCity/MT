// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class ThrottlingSettings
    {
        [Optional] public int MarginCallThrottlingPeriodMin { get; set; } = 30;
        
        [Optional] public int StopOutThrottlingPeriodMin { get; set; } = 1;
    }
}