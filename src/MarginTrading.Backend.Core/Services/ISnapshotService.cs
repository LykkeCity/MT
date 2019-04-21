using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISnapshotService
    {
        Task<string> MakeTradingDataSnapshot(string correlationId);
    }
}