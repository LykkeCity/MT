// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Common.Log;

namespace MarginTrading.Common.Services
{
    public static class LogLocator
    {
        public static ILog CommonLog { get; set; }
        public static ILog RequestsLog { get; set; }
    }
}
