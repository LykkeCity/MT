using System.Threading.Tasks;
using System.Collections.Generic;
using MarginTrading.Core;
using MarginTrading.Core.Notifications;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
	public class RiskManager : IRiskManager
	{
		private IDictionary<string, double> _pVaRSoftLimits;
		private IDictionary<string, double> _pVaRHardLimits;

		private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
		private readonly ISlackNotificationsProducer _slackNotificationsProducer;

		public RiskManager(IRabbitMqNotifyService rabbitMqNotifyService,
			ISlackNotificationsProducer slackNotificationsProducer,
			IDictionary<string, double> pVaRSoftLimits,
			IDictionary<string, double> pVaRHardLimits)
		{
			_pVaRHardLimits = pVaRHardLimits;
			_pVaRSoftLimits = pVaRSoftLimits;
			_rabbitMqNotifyService = rabbitMqNotifyService;
			_slackNotificationsProducer = slackNotificationsProducer;
		}

		public async Task CheckLimit(string counterPartyId, double pVaREstimate)
		{
			await CheckHardLimit(counterPartyId, pVaREstimate);
			await CheckSoftLimit(counterPartyId, pVaREstimate);
		}

		private async Task CheckSoftLimit(string counterPartyId, double pVaREstimate)
		{
			if (_pVaRSoftLimits.ContainsKey(counterPartyId)
				&& _pVaRSoftLimits[counterPartyId] < pVaREstimate)
			{
				await _slackNotificationsProducer.SendNotification(ChannelTypes.MarginTrading, $"Soft pVaR Limit Reached for the client: { counterPartyId }. Soft limit for the client is set to { _pVaRSoftLimits[counterPartyId] }, and actual value at risk is estimated as { pVaREstimate }", "Margin Trading Risk Management Module");
			}
		}

		private async Task CheckHardLimit(string counterPartyId, double pVaREstimate)
		{
			if (_pVaRHardLimits.ContainsKey(counterPartyId)
				&& _pVaRHardLimits[counterPartyId] < pVaREstimate)
			{
				await _rabbitMqNotifyService.HardTradingLimitReached(counterPartyId);
				await _slackNotificationsProducer.SendNotification(ChannelTypes.MarginTrading, $"Hard pVaR Limit Reached for the client: { counterPartyId }. Hard limit for the client is set to { _pVaRHardLimits[counterPartyId] }, and actual value at risk is estimated as { pVaREstimate }", "Margin Trading Risk Management Module");
			}
		}

		public async Task CheckLimits(IDictionary<string, double> pVaR)
		{
			foreach (KeyValuePair<string, double> kvp in pVaR)
			{
				string counterpartyId = kvp.Key;
				double pVaREstimate = kvp.Value;

				await CheckLimit(counterpartyId, pVaREstimate);
			}
		}
	}
}
