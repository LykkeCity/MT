using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.BackendContracts
{
    public class DepositWithdrawBackendRequest
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public double Volume { get; set; }
        public bool IsLive { get; set; }
    }
}
