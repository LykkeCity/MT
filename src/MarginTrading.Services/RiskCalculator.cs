using MarginTrading.Core;
using MarginTrading.Core.Notifications;
using MarginTrading.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Services
{
	public class RiskCalculator : IRiskCalculator
	{
		private const double q5 = 1.959963984540;
		private readonly double _T;
		private readonly bool _enforceCalculation;
		private readonly ISlackNotificationsProducer _slackNotificationProducer;
		private Dictionary<string, double> _s;
		private Dictionary<string, double> _m;
		private Dictionary<string, Dictionary<string, double>> _corrMatrix;
		private Dictionary<string, Dictionary<string, double>> _corrMatrixEnforced;
		private Dictionary<string, Dictionary<string, double>> _iVaR;
		private Dictionary<string, double> _pVaR;
		private IEnumerable<string> _currencies;
		private IEnumerable<string> _clients;

		public RiskCalculator(
			double frequency,
			bool enforceCalculation,
			ISlackNotificationsProducer slackNotificationProducer,
			Dictionary<string, Dictionary<string, double>> corrMatrix = null)
		{
			_slackNotificationProducer = slackNotificationProducer;
			_s = new Dictionary<string, double>();
			_m = new Dictionary<string, double>();
			_iVaR = new Dictionary<string, Dictionary<string, double>>();
			_corrMatrix = new Dictionary<string, Dictionary<string, double>>();
			_pVaR = new Dictionary<string, double>();
			_T = frequency;
			_enforceCalculation = enforceCalculation;
			if (enforceCalculation && corrMatrix != null)
			{
				_corrMatrixEnforced = corrMatrix;
			}
		}

		public IDictionary<string, double> PVaR
		{
			get
			{
				return _pVaR;
			}
		}

		public void InitializeAsync(
			IEnumerable<string> clientIDs,
			IEnumerable<string> currencies,
			Func<string, double[]> getMeanUsdQuoteVector,
			Func<string, OrderDirection, double?> getLatestUsdQuote,
			Func<string, string, Func<string, OrderDirection, double?>, double?> getEquivalentUsdPosition)
		{
			_clients = clientIDs;
			_currencies = currencies.Where(x => x != "USD");

			foreach (string currencyA in _currencies)
			{
				_s[currencyA] = 0;
				_m[currencyA] = 0;
				_corrMatrix[currencyA] = new Dictionary<string, double>();

				foreach (string currencyB in _currencies)
				{
					_corrMatrix[currencyA][currencyB] = currencyA == currencyB ? 1 : 0;
					if (_enforceCalculation)
					{
						if (_corrMatrixEnforced != null && _corrMatrixEnforced.ContainsKey(currencyA) && _corrMatrixEnforced[currencyA].ContainsKey(currencyB))
						{
							_corrMatrix[currencyA][currencyB] = _corrMatrixEnforced[currencyA][currencyB];
						}
					}
				}
			}

			foreach (string client in _clients)
			{
				_iVaR[client] = new Dictionary<string, double>();

				foreach (string currencyA in _currencies)
				{
					_iVaR[client][currencyA] = 0;
				}

				_pVaR[client] = 0;
			}

			UpdateInternalStateAsync(getMeanUsdQuoteVector, getLatestUsdQuote, getEquivalentUsdPosition);
		}

		public void UpdateInternalStateAsync(Func<string, double[]> getMeanUsdQuoteVector,
			Func<string, OrderDirection, double?> getLatestUsdQuote,
			Func<string, string, Func<string, OrderDirection, double?>, double?> getEquivalentUsdPosition)
		{
			foreach (string currencyA in _currencies)
			{
				double[] ratesA = getMeanUsdQuoteVector(currencyA);

				if (ratesA == null)
				{
					_slackNotificationProducer.SendNotification(ChannelTypes.MarginTrading, $"There are no USD quotes for the asset {currencyA}", "Margin Trading Risk Management Module");
					continue;
				}

				_s[currencyA] = StDevLogaritmicReturn(ratesA, _T);
				_m[currencyA] = MeanLogaritmicReturn(ratesA);
			}

			foreach (string currencyA in _currencies)
			{
				double[] ratesA = getMeanUsdQuoteVector(currencyA);

				if (ratesA == null)
				{
					continue;
				}

				foreach (string currencyB in _currencies)
				{
					if (_enforceCalculation)
					{
						if (_corrMatrixEnforced != null && _corrMatrixEnforced.ContainsKey(currencyA) && _corrMatrixEnforced[currencyA].ContainsKey(currencyB))
						{
							continue;
						}
					}

					double[] ratesB = getMeanUsdQuoteVector(currencyB);

					if (ratesB == null)
					{
						_slackNotificationProducer.SendNotification(ChannelTypes.MarginTrading, $"There are no USD quotes for the asset {currencyB}", "Margin Trading Risk Management Module");
						continue;
					}

					CorrMatrixMember(currencyA, currencyB, ratesA, ratesB);
				}

				foreach (string client in _clients)
				{
					double? usdPositionVolume = getEquivalentUsdPosition(client, currencyA, getLatestUsdQuote);

					if (usdPositionVolume.HasValue)
					{
						IVaR(client, currencyA, usdPositionVolume.Value);
					}
				}
			}

			foreach (string clientId in _clients)
			{
				CalculatePVaR(clientId);
			}
		}

		public void RecalculatePVaR(string counterPartyId, string asset, double? equivalentUsdPositionVolume)
		{
			if (equivalentUsdPositionVolume.HasValue)
			{
				IVaR(counterPartyId, asset, equivalentUsdPositionVolume.Value);

				CalculatePVaR(counterPartyId);
			}
		}

		private void CalculatePVaR(string client)
		{
			double pVaRSquare = 0;

			foreach (string currencyA in _currencies)
			{
				foreach (string currencyB in _currencies)
				{
					pVaRSquare += _iVaR[client][currencyA] * _corrMatrix[currencyB][currencyA] * _iVaR[client][currencyB];
				}
			}

			_pVaR[client] = Math.Sqrt(pVaRSquare);
		}

		private double StDevLogaritmicReturn(double[] rates, double T)
		{
			return Math.Sqrt(
					MeanLogaritmicReturnOfSquares(rates) - (MeanLogaritmicReturn(rates) * MeanLogaritmicReturn(rates))
				);
		}

		private double MeanLogaritmicReturn(double[] rates)
		{
			double M = 0;

			int N = rates.Length;

			for (int i = 1; i < N; i++)
			{
				M += Math.Log(rates[i] / rates[i - 1]);
			}

			return M / (N - 1);
		}

		private double MeanLogaritmicReturnOfSquares(double[] rates)
		{
			double M = 0;

			int N = rates.Length;

			for (int i = 1; i < N; i++)
			{
				M += Math.Log(rates[i] / rates[i - 1]) * Math.Log(rates[i] / rates[i - 1]);
			}

			return M / (N - 1);
		}

		private double IVaR(double Q, double S, double M)
		{
			return Q * (M - S) * q5;
		}

		private double Cov(double[] ratesA, double[] ratesB)
		{
			int n = ratesA.Length <= ratesB.Length ? ratesA.Length : ratesB.Length;

			double Ea = MeanLogaritmicReturn(ratesA);
			double Eb = MeanLogaritmicReturn(ratesB);

			double C = 0;

			for (int i = 1; i < n; i++)
			{
				double x = (Math.Log(ratesA[i] / ratesA[i - 1]));
				double y = (Math.Log(ratesB[i] / ratesB[i - 1]));

				C += (x * y) - (x * Eb) - (y * Ea) + (Ea * Eb);
			}

			double cov = C / (n - 1);

			return cov;
		}

		private double Mean(double[] rates)
		{
			return rates.Sum() / rates.Length;
		}

		private void CorrMatrixMember(string instrumentA, string instrumentB, double[] ratesA, double[] ratesB)
		{
			_corrMatrix[instrumentA][instrumentB] = Cov(ratesA, ratesB) / (_s[instrumentA] * _s[instrumentB]);
		}

		private void IVaR(string clientId, string instrument, double usdPositionValue)
		{
			_iVaR[clientId][instrument] = IVaR(usdPositionValue, _s[instrument], _m[instrument]);
		}
	}
}