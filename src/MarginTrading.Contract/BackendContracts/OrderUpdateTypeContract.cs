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