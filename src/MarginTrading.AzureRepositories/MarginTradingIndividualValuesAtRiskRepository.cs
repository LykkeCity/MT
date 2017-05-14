using AzureStorage;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
	public class MarginTradingIndividualValuesAtRiskRepository : IMarginTradingIndividualValuesAtRiskRepository
	{
		private readonly INoSQLTableStorage<IndividualValueAtRiskEntity> _tableStorage;

		public MarginTradingIndividualValuesAtRiskRepository(INoSQLTableStorage<IndividualValueAtRiskEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public async Task InsertOrUpdateAsync(string counterPartyId, string assetId, double value)
		{
			await _tableStorage.InsertOrReplaceAsync(IndividualValueAtRiskEntity.Create(counterPartyId, assetId, value));
		}
	}
}
