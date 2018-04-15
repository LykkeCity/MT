using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.Migrations
{
    public interface IMigration
    {
        int Version { get; }

        Task Invoke();
    }
}