using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Backend.Core
{
    public class OvernightSwapNotification
    {
        public string CliendId { get; set; }
        public IReadOnlyList<AccountCalculations> CalculationsByAccount { get; set; }
        public string CurrentDate => DateTime.UtcNow.ToString("dd MMMM yyyy");

        public class AccountCalculations
        {
            public string AccountId { get; set; }
            public string AccountCurrency { get; set; }
            public IReadOnlyList<SingleCalculation> Calculations { get; set; }
            public decimal TotalCost => Calculations.Sum(x => x.Cost);
        }
        
        public class SingleCalculation
        {
            public string Instrument { get; set; }
            public string Direction { get; set; }
            public decimal Volume { get; set; }
            public decimal SwapRate { get; set; }
            public decimal Cost { get; set; }
            public List<string> PositionIds { get; set; }
            public string PositionIdsString => string.Join("<br />", PositionIds);
        }
    }
}