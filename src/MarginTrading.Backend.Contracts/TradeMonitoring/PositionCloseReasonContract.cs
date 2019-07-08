// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public enum PositionCloseReasonContract
    {
        None,
        Close,
        StopLoss,
        TakeProfit,
        StopOut
    }
}
