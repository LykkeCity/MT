using AzureStorage;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
	public class MarginTradingAggregateValuesAtRiskRepository : IMarginTradingAggregateValuesAtRiskRepository
	{
		private readonly INoSQLTableStorage<AggregateValueAtRiskEntity> _tableStorage;

		public MarginTradingAggregateValuesAtRiskRepository(INoSQLTableStorage<AggregateValueAtRiskEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public async Task InsertOrUpdateAsync(string counterPartyId, double value)
		{
			await _tableStorage.InsertOrReplaceAsync(AggregateValueAtRiskEntity.Create(counterPartyId, value));
		}
	}
}
