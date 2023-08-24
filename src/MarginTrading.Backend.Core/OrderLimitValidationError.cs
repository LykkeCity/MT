// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core
{
    public enum OrderLimitValidationError
    {
        None,
        OneTimeLimit,
        TotalLimit,
        MaxPositionNotionalLimit
    }
}