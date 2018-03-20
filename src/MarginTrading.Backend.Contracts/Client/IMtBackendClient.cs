﻿using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Client
{
    [PublicAPI]
    public interface IMtBackendClient
    {
        /// <summary>
        /// Manages day offs schedule and exclusions
        /// </summary>
        IScheduleSettingsApi ScheduleSettings { get; }
        
        /// <summary>
        /// Account deposit, withdraw and other operations with balace
        /// </summary>
        IAccountsBalanceApi AccountsBalance { get; }

        IAssetPairSettingsEditingApi AssetPairSettingsEdit { get; }
    }
}