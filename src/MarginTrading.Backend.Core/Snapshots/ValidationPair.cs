// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Snapshots
{
    public class ValidationPair<T>
    {
        public T Restored { get; set; }
        
        public T Current { get; set; }
    }
}