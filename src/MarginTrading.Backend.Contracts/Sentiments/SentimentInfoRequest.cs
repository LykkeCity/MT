// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.Sentiments
{
    public class SentimentInfoRequest
    {
        public HashSet<string> ProductIds { get; set; }
    }
}