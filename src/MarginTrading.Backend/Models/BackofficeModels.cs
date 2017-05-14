using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace MarginTrading.Backend.Models
{
    public class SummaryAssetInfo
    {
        public string AssetPairId { get; set; }
        public double VolumeLong { get; set; }
        public double VolumeShort { get; set; }
        public double PnL { get; set; }
    }

    public class SetTradingConditionModel
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
    }

    public class CreateMarginTradingAccountModel
    {
        [Required]
        public string ClientId { get; set; }
        [Required]
        public string AssetId { get; set; }
        [Required]
        public string TradingConditionId { get; set; }
    }

    public class AssignInstrumentsModel
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string[] Instruments { get; set; }
    }

    public class InitAccountsRequest
    {
        public string ClientId { get; set; }
        public string TradingConditionsId { get; set; }
    }

    public class InitAccountsResponse
    {
        public CreateAccountStatus Status { get; set; }
        public string Message { get; set; }
    }

    public enum CreateAccountStatus
    {
        Available,
        Created,
        Error
    }
}
