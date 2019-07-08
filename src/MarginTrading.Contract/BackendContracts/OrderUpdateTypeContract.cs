// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.Contract.BackendContracts
{
    [PublicAPI]
    public enum OrderUpdateTypeContract
    {
        Place,
        Cancel,
        Activate,
        Reject,
        Closing,
        Close,
        ChangeOrderLimits,
    }
}