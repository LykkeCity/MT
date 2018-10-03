using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// Api to prepare data for reports
    /// </summary>
    [PublicAPI]
    public interface IReportApi
    {
        /// <summary>
        /// Populates the data needed for report building to the storage: open positions and account fpl
        /// </summary>
        /// <returns>Returns 200 on success, exception otherwise</returns>
        [Post("/api/reports/dump-data")]
        Task DumpReportData();
    }
}