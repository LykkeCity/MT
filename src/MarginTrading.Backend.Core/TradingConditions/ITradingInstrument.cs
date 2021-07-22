// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Snow.Common;
using Lykke.Snow.Common.Percents;

namespace MarginTrading.Backend.Core.TradingConditions
{
    public interface ITradingInstrument
    {
        string TradingConditionId { get; }
        string Instrument { get; }
        
        decimal SwapLong { get; }
        decimal SwapShort { get; }
        
        decimal Delta { get; }
        decimal DealMinLimit { get; }
        decimal DealMaxLimit { get; }
        decimal PositionLimit { get; }
        bool ShortPosition { get; }
        decimal OvernightMarginMultiplier { get; }
        
        decimal CommissionRate { get; }
        decimal CommissionMin { get; }
        decimal CommissionMax { get; }
        string CommissionCurrency { get; }
        
        Leverage InitLeverage { get; }
        Leverage MaintenanceLeverage { get; }
        MarginRate MarginRate { get; }

        public decimal GetMarginInitByLeverage(bool isOvernightMarginParameterOn, bool isWarnCheck);
        public decimal GetMarginMaintenanceByLeverage(bool isOvernightMarginParameterOn, bool isWarnCheck);
    }
}
