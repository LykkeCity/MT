// Copyright (c) 2019 Lykke Corp.

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