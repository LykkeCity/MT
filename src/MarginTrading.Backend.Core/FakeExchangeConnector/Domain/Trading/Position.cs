namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading
{
    public class Position
    {
        public string Symbol { get; set; }

        public decimal PositionVolume { get; set; }

        public decimal MaintMarginUsed { get; set; }

        public decimal RealisedPnL { get; set; }

        public decimal UnrealisedPnL { get; set; }
        
        public decimal? Value { get; set; }

        public decimal? AvailableMargin { get; set; }

        public decimal InitialMarginRequirement { get; set; }

        public decimal MaintenanceMarginRequirement { get; set; }

        public Position Clone()
        {
            return new Position
            {
                Symbol = this.Symbol,
                PositionVolume = this.PositionVolume,
                MaintMarginUsed = this.MaintMarginUsed,
                RealisedPnL = this.RealisedPnL,
                UnrealisedPnL = this.UnrealisedPnL,
                Value = this.Value,
                AvailableMargin = this.AvailableMargin,
                InitialMarginRequirement = this.InitialMarginRequirement,
                MaintenanceMarginRequirement = this.MaintenanceMarginRequirement
            };
        }
    }
}
