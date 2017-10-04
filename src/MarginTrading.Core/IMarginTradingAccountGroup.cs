using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingAccountGroup
    {
        string TradingConditionId { get; }
        string BaseAssetId { get; }
        decimal MarginCall { get; }
        decimal StopOut { get; }
        decimal DepositTransferLimit { get; }
    }

    public class MarginTradingAccountGroup : IMarginTradingAccountGroup
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal MarginCall { get; set; }
        public decimal StopOut { get; set; }
        public decimal DepositTransferLimit { get; set; }

        public static MarginTradingAccountGroup Create(IMarginTradingAccountGroup src)
        {
            return new MarginTradingAccountGroup
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                MarginCall = src.MarginCall,
                StopOut = src.StopOut,
                DepositTransferLimit = src.DepositTransferLimit
            };
        }
    }

    public interface IMarginTradingAccountGroupRepository
    {
        Task AddOrReplaceAsync(IMarginTradingAccountGroup group);
        Task<IMarginTradingAccountGroup> GetAsync(string tradingConditionId, string baseAssetId);
        Task<IEnumerable<IMarginTradingAccountGroup>> GetAllAsync();
    }
}
