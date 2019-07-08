// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.ExternalOrderBroker.Models;

namespace MarginTrading.ExternalOrderBroker.Repositories
{
	public interface IExternalOrderReportRepository
	{
		Task InsertOrReplaceAsync(IExternalOrderReport entity);
	}
}