using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.AzureRepositories.Snow.OrdersById
{
    public interface IOrdersByIdRepository
    {
        [ItemCanBeNull]
        Task<IOrderById> GetAsync(string id);

        Task TryInsertAsync(IOrderById order);
    }
}