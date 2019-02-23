using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IOvernightMarginRepository
    {
        /// <summary>
        /// Read overnight margin parameter value from persistent storage.
        /// </summary>
        decimal ReadOvernightMarginParameter();

        /// <summary>
        /// Write overnight margin parameter value to persistent storage.
        /// </summary>
        Task WriteOvernightMarginParameterAsync(decimal newValue);
    }
}