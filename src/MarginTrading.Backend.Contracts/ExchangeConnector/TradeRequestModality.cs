// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.ExchangeConnector
{
    public enum TradeRequestModality
    {
        Unspecified = 0,
        Liquidation_CorporateAction = 76,
        Regular = 82,
        Liquidation_MarginCall = 108
    }
}