using System;
using System.ComponentModel;

namespace MarginTrading.Contract.ClientContracts
{
    [DisplayName("Account info")]
    public class OrderClientContract
    {
        [DisplayName("Order id")]
        public string Id { get; set; }
        [DisplayName("Account id")]
        public string AccountId { get; set; }
        [DisplayName("Instrument (asset pair) id")]
        public string Instrument { get; set; }
        [DisplayName("Contract direction (buy - 0, sell - 1)")]
        public int Type { get; set; }
        [DisplayName("Order status (WaitingForExecution - 0, Active - 1, Closed - 2, Rejected - 3, Closing - 4)")]
        public int Status { get; set; }
        [DisplayName("Order close reason (None - 0, Close - 1, StopLoss - 2, TakeProfit - 3, StopOut - 4, " +
                     "Canceled - 5, CanceledBySystem - 6, ClosedByBroker - 7)")]
        public int CloseReason { get; set; }
        [DisplayName("Order reject reason (None - 0, NoLiquidity - 1, NotEnoughBalance - 2, LeadToStopOut - 3, " +
                     "AccountInvalidState - 4, InvalidExpectedOpenPrice - 5, InvalidVolume - 6, " +
                     "InvalidTakeProfit - 7, InvalidStoploss - 8, InvalidInstrument = 9, InvalidAccount - 10, " +
                     "TradingConditionError - 11, TechnicalError - 12)")]
        public int RejectReason { get; set; }
        [DisplayName("Order reject reason in human-readable text")]
        public string RejectReasonText { get; set; }
        [DisplayName("Order expected open price")]
        public decimal? ExpectedOpenPrice { get; set; }
        [DisplayName("Order actual open price")]
        public decimal OpenPrice { get; set; }
        [DisplayName("Order actual close price")]
        public decimal ClosePrice { get; set; }
        [DisplayName("Order actual open date & time")]
        public DateTime? OpenDate { get; set; }
        [DisplayName("Order actual close date & time")]
        public DateTime? CloseDate { get; set; }
        [DisplayName("Order volume")]
        public decimal Volume { get; set; }
        [DisplayName("Order matched volume")]
        public decimal MatchedVolume { get; set; }
        [DisplayName("Order close volume")]
        public decimal MatchedCloseVolume { get; set; }
        [DisplayName("Order take profit")]
        public decimal? TakeProfit { get; set; }
        [DisplayName("Order stop loss")]
        public decimal? StopLoss { get; set; }
        [DisplayName("Order floating profit loss")]
        public decimal? Fpl { get; set; }
        [DisplayName("Order total floating profit loss")]
        public decimal? TotalPnL { get; set; }
        [DisplayName("Order open comission")]
        public decimal OpenCommission { get; set; }
        [DisplayName("Order close comission")]
        public decimal CloseCommission { get; set; }
        [DisplayName("Order swap comission")]
        public decimal SwapCommission { get; set; }
    }
}
