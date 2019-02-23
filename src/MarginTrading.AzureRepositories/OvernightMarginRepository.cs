using System.Threading.Tasks;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.AzureRepositories
{
    public class OvernightMarginRepository: IOvernightMarginRepository
    {
        public decimal ReadOvernightMarginParameter()
        {
            throw new System.NotImplementedException();
        }

        public Task WriteOvernightMarginParameterAsync(decimal newValue)
        {
            throw new System.NotImplementedException();
        }
    }
}