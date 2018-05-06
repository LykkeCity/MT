using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Positions;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// API for performing operations with positions
    /// </summary>
    [PublicAPI]
    public interface IPositionsApi
    {
        [Delete("/api/positions")]
        Task CloseAsync([Body] PositionCloseRequest request);

    }
}