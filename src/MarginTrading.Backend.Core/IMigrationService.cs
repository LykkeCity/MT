using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    /// <summary>
    /// The service gather all descendants of AbstractMigration class and invoke them in StartApplicationAsync.
    /// </summary>
    public interface IMigrationService
    {
        Task InvokeAll();
    }
}