using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IAccountStatRepository
    {
        Task Dump(IEnumerable<MarginTradingAccount> accounts);
    }
}