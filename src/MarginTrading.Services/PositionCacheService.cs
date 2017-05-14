using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
	public class PositionCacheService : IPositionCacheService
	{
		private IDictionary<string, IDictionary<string, IPosition>> _cache;
		private List<string> _clients;
		private List<string> _currencies;
		private static object _lock = new object();

		public bool IsInitialized
		{
			get
			{
				return _cache != null;
			}
		}

		public IEnumerable<string> ClientIDs
		{
			get
			{
				return _clients;
			}
		}

		public IEnumerable<string> Currencies
		{
			get
			{
				return _currencies;
			}
		}

		public IPosition GetPosition(string party, string asset)
		{
			lock (_lock)
			{
				if (_cache == null)
					return null;

				if (!_cache.ContainsKey(party))
					return null;

				if (!_cache[party].ContainsKey(asset))
					return null;

				return _cache[party][asset];
			}
		}

		public IEnumerable<IPosition> GetPositions()
		{
			lock (_lock)
			{
				List<IPosition> positions = new List<IPosition>();

				if (_cache == null)
					return positions;

				foreach (IDictionary<string, IPosition> clientPositions in _cache.Values)
				{
					if (clientPositions == null)
						continue;

					foreach (IPosition position in clientPositions.Values)
					{
						positions.Add(position);
					}
				}

				return positions;
			}
		}

		public IEnumerable<IPosition> GetPositions(string party)
		{
			lock (_lock)
			{
				List<IPosition> positions = new List<IPosition>();

				if (_cache == null)
					return positions;

				if (!_cache.ContainsKey(party))
					return positions;

				foreach (IPosition position in _cache[party].Values)
				{
					positions.Add(position);
				}

				return positions;
			}
		}

		public async Task Initialize(Func<Task<IEnumerable<IPosition>>> source)
		{
			_currencies = new List<string>();
			_clients = new List<string>();
			_cache = new Dictionary<string, IDictionary<string, IPosition>>();

			foreach (IPosition position in await source())
			{
				UpdatePosition(position);
			}
		}

		public void UpdatePosition(IPosition position)
		{
			lock (_lock)
			{
				if (_cache == null)
				{
					_cache = new Dictionary<string, IDictionary<string, IPosition>>();
				}

				if (!_cache.ContainsKey(position.ClientId))
				{
					_cache[position.ClientId] = new Dictionary<string, IPosition>();
				}

				_cache[position.ClientId][position.Asset] = position;

				if (!_clients.Contains(position.ClientId))
				{
					_clients.Add(position.ClientId);
				}

				if (!_currencies.Contains(position.Asset))
				{
					_currencies.Add(position.Asset);
				}
			}
		}

		public async Task InitializeFromTransactions(Func<Task<IEnumerable<IElementaryTransaction>>> source)
		{
			_currencies = new List<string>();
			_clients = new List<string>();
			_cache = new Dictionary<string, IDictionary<string, IPosition>>();

			foreach (IElementaryTransaction transaction in await source())
			{
				if (transaction.Type == ElementaryTransactionType.Position)
				{
					UpdatePosition(transaction);
				}
			}
		}

		public void UpdatePosition(IElementaryTransaction transaction)
		{
			lock (_lock)
			{
				if (_cache == null)
				{
					_cache = new Dictionary<string, IDictionary<string, IPosition>>();
				}

				if (!_cache.ContainsKey(transaction.CounterPartyId))
				{
					_cache[transaction.CounterPartyId] = new Dictionary<string, IPosition>();
				}

				if (!_cache[transaction.CounterPartyId].ContainsKey(transaction.Asset))
				{
					_cache[transaction.CounterPartyId][transaction.Asset] = new Position { ClientId = transaction.CounterPartyId, Asset = transaction.Asset, Volume = 0 };
				}

				if (transaction.Amount.HasValue)
				{
					_cache[transaction.CounterPartyId][transaction.Asset].Volume += transaction.Amount.Value;
				}

				if (!_clients.Contains(transaction.CounterPartyId))
				{
					_clients.Add(transaction.CounterPartyId);
				}

				if (!_currencies.Contains(transaction.Asset))
				{
					_currencies.Add(transaction.Asset);
				}
			}
		}
	}
}