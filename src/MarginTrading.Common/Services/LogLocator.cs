// Copyright (c) 2019 Lykke Corp.

using Common.Log;

namespace MarginTrading.Common.Services
{
    public static class LogLocator
    {
        public static ILog CommonLog { get; set; }
        public static ILog RequestsLog { get; set; }
    }
}
