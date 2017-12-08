using System.Threading.Tasks;

namespace MarginTrading.Frontend.Repositories.Contract
{
    public interface IMaintenanceInfoRepository
    {
        Task<IMaintenanceInfo> GetMaintenanceInfo(bool isLive);
    }
}