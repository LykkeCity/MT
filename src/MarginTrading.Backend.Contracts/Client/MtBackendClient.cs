using Lykke.ClientGenerator;

namespace MarginTrading.Backend.Contracts.Client
{
    internal class MtBackendClient : IMtBackendClient
    {
        public IScheduleSettingsApi ScheduleSettings { get; }

        public IAccountsBalanceApi AccountsBalance { get; }

        public IAssetPairsEditingApi AssetPairsEdit { get; }

        public ITradingConditionsEditingApi TradingConditionsEdit { get; }

        public MtBackendClient(IClientProxyGenerator clientProxyGenerator)
        {
            ScheduleSettings = clientProxyGenerator.Generate<IScheduleSettingsApi>();
            AccountsBalance = clientProxyGenerator.Generate<IAccountsBalanceApi>();
            AssetPairsEdit = clientProxyGenerator.Generate<IAssetPairsEditingApi>();
            TradingConditionsEdit = clientProxyGenerator.Generate<ITradingConditionsEditingApi>();
        }
    }
}