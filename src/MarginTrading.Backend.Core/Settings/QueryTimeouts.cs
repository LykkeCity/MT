using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class QueryTimeouts
    {
        [Optional]
        public int GetLastSnapshotTimeoutS { get; set; } = 120;
    }
}
