using System.Threading.Tasks;
using MarginTrading.ExternalOrderBroker.Models;

namespace MarginTrading.ExternalOrderBroker.Repositories
{
	public interface IExternalOrderReportRepository
	{
		Task InsertOrReplaceAsync(IExternalOrderReport entity);
	}
}