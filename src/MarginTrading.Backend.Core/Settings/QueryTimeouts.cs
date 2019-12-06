using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class QueryTimeouts
    {
        public int GetLastSnapshotTimeoutS { get; set; }
    }
}
