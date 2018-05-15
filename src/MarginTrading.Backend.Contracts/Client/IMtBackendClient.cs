using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Client
{
    [PublicAPI]
    public interface IMtBackendClient
    {
        /// <summary>
        /// Performing operations with orders
        /// </summary>
        IOrdersApi Orders { get; }
        
        /// <summary>
        /// Performing operations with positions
        /// </summary>
        IPositionsApi Positions { get; }
    }
}