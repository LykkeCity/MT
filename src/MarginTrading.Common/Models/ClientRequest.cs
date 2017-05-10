using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.Models
{
    public class ClientRequest
    {
        [Required]
        public string Token { get; set; }
        public string ClientId { get; set; }
    }
}
