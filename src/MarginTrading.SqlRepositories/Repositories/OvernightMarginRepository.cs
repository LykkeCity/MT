using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class OvernightMarginRepository: IOvernightMarginRepository
    {
        private readonly IMarginTradingBlobRepository _marginTradingBlobRepository;

        private const decimal OvernightMarginParameterDefault = 3;
        private const string OvernightMarginParameterBlobName = "OvernightMarginParameter";
        
        public OvernightMarginRepository(
            IMarginTradingBlobRepository marginTradingBlobRepository)
        {
            _marginTradingBlobRepository = marginTradingBlobRepository;
        }

        public decimal ReadOvernightMarginParameter()
        {
            var blobValue = _marginTradingBlobRepository.Read<decimal?>(LykkeConstants.StateBlobContainer, 
                OvernightMarginParameterBlobName);

            if (blobValue != null)
            {
                return blobValue.Value;
            }
            
            WriteOvernightMarginParameterAsync(OvernightMarginParameterDefault).GetAwaiter().GetResult();
            
            return OvernightMarginParameterDefault;
        }

        public async Task WriteOvernightMarginParameterAsync(decimal newValue)
        {
            await _marginTradingBlobRepository.Write(LykkeConstants.StateBlobContainer, 
                OvernightMarginParameterBlobName, newValue);
        }
    }
}