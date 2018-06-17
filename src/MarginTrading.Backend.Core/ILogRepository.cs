using System.Threading.Tasks;
using Lykke.Logs;

namespace MarginTrading.Backend.Core
{
    public interface ILogRepository
    {
        Task Insert(ILogEntity log);
    }
}