using System.Threading.Tasks;

namespace MarginTrading.BrokerBase
{
    public interface IBrokerApplication
    {
        Task RunAsync();
        void StopApplication();
    }
}