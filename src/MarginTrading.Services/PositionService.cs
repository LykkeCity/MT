using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
	public class PositionService : IPositionService
	{
		private readonly IPositionCacheService _positionCache;
		private readonly IMarginTradingPositionRepository _positionRepository;
		private readonly IElementaryTransactionsRepository _elementaryTransactionRepository;

		public IEnumerable<string> ClientIDs
		{
			get
			{
				return _positionCache.ClientIDs;
			}
		}

		public IEnumerable<string> Currencies
		{
			get
			{
				return _positionCache.Currencies;
			}
		}

		public PositionService(IPositionCacheService positionCache,
				IMarginTradingPositionRepository positionRepository,
				IElementaryTransactionsRepository elementaryTransactionRepository)
		{
			_elementaryTransactionRepository = elementaryTransactionRepository;
			_positionCache = positionCache;
			_positionRepository = positionRepository;
		}

		public async Task InitializeAsync()
		{
			if (_positionRepository.Any())
			{
				await _positionCache.Initialize(_positionRepository.GetAllAsync);
			}
			else if (_elementaryTransactionRepository.Any())
			{
				await _positionCache.InitializeFromTransactions(_elementaryTransactionRepository.GetAllAsync);
			}
		}

		public double? GetEquivalentUsdPosition(string clientId, string currency, Func<string, OrderDirection, double?> getCurrentUsdQuoteForAsset)
		{
			var position = _positionCache.GetPosition(clientId, currency);

			if (position != null)
			{
				double positionVolume = position.Volume;

				OrderDirection direction = positionVolume >= 0 ? OrderDirection.Sell : OrderDirection.Buy;

				double? price = getCurrentUsdQuoteForAsset(currency, direction);

				if (price.HasValue)
				{
					return position.Volume * price;
				}
			}

			return 0;
		}

		public IPosition ProcessTransaction(IElementaryTransaction transaction)
		{
			if (transaction != null
			 && transaction.Type == ElementaryTransactionType.Position
			 && transaction.Amount.HasValue
			 && transaction.Amount.Value != 0)
			{
				IPosition position = _positionCache.GetPosition(transaction.CounterPartyId, transaction.Asset);

				if (position == null)
				{
					position = new Position()
					{
						ClientId = transaction.CounterPartyId,
						Asset = transaction.Asset
					};
				}

				position.Volume += transaction.Amount.Value;

				_positionCache.UpdatePosition(position);

				return position;
			}

			return null;
		}

		public async Task SavePositions()
		{
			foreach (IPosition position in _positionCache.GetPositions())
			{
				await _positionRepository.UpdateAsync(position);
			}
		}
	}
}
