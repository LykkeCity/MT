using System.Threading.Tasks;
using MarginTrading.Common.Settings.Models;

namespace MarginTrading.Common.Settings.Repositories
{
    public interface IAppGlobalSettingsRepositry
    {
        Task SaveAsync(IAppGlobalSettings appGlobalSettings);

        Task UpdateAsync(string depositUrl = null, bool? debugMode = null,
            string defaultIosAssetGroup = null, string defaultAssetGroupForOther = null,
            double? minVersionOnReview = null, string reviewIosGroup = null, bool? isOnReview = null,
            double? icoLkkSold = null, bool? isOnMaintenance = null, int? lowCashOutTimeout = null,
            int? lowCashOutLimit = null, bool? marginTradingEnabled = null);

        Task<IAppGlobalSettings> GetAsync();
    }
}