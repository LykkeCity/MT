// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Percents;

namespace MarginTrading.Backend.Core.TradingConditions
{
    [UsedImplicitly]
    public class TradingInstrument : ITradingInstrument
    {
        public string TradingConditionId { get; set; }
        public string Instrument { get; set; }
        public decimal SwapLong { get; set; }
        public decimal SwapShort { get; set; }
        public decimal Delta { get; set; }
        public decimal DealMinLimit { get; set; }
        public decimal DealMaxLimit { get; set; }
        public decimal PositionLimit { get; set; }
        public bool ShortPosition { get; set; }
        public decimal OvernightMarginMultiplier { get; set; }
        
        public decimal CommissionRate { get; set; }
        public decimal CommissionMin { get; set; }
        public decimal CommissionMax { get; set; }
        public string CommissionCurrency { get; set; }
        public Leverage InitLeverage { get; set; }
        public Leverage MaintenanceLeverage { get; set; }
        // TODO: Probably, MarginRate should be used instead of margin init and margin maintenance. This value comes from
        // asset service and is the source whereas margin init and maring maintenance are calculated based on leverage
        public MarginRate MarginRate { get; set; }

        public (string, string) GetKey() => (TradingConditionId, Instrument);

        public decimal GetMarginInitByLeverage(bool isOvernightMarginParameterOn, bool isWarnCheck) =>
            GetMarginRateByLeverage(isOvernightMarginParameterOn, isWarnCheck, InitLeverage);

        public decimal GetMarginMaintenanceByLeverage(bool isOvernightMarginParameterOn, bool isWarnCheck) =>
            GetMarginRateByLeverage(isOvernightMarginParameterOn, isWarnCheck, MaintenanceLeverage);

        private decimal GetMarginRateByLeverage(bool isOvernightMarginParameterOn, bool isWarnCheck, decimal leverage)
        {
            var nominator = GetMarginCalculationNominator(isOvernightMarginParameterOn, isWarnCheck);

            return nominator / leverage;
        }

        private decimal GetMarginCalculationNominator(bool isOvernightMarginParameterOn, bool isWarnCheck) =>
            (isWarnCheck || isOvernightMarginParameterOn) && OvernightMarginMultiplier > 1
                ? OvernightMarginMultiplier
                : 1;
    }
}