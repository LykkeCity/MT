using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Services
{
    public interface IReportService
    {
        Task DumpReportData();
    }
}