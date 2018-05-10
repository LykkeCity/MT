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
        [Delete("/api/positions/{positionId}")]
        Task CloseAsync(string positionId, PositionCloseRequest request);

        [Delete("/api/positions/instrument-group/{instrumentId}")]
        Task CloseGroupAsync(string instrument, PositionCloseRequest request,
            PositionDirectionContract? direction = null);
    }
}