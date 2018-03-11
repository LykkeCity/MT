using Refit;

namespace MarginTrading.Backend.Contracts.Client
{
    internal class MtBackendClient : IMtBackendClient
    {
        public IScheduleSettingsApi ScheduleSettings { get; }
        
        public IAccountsBalanceApi AccountsBalance { get; }

        public MtBackendClient(string url, string apiKey, string userAgent)
        {
            var httpMessageHandler = new MtBackendHttpClientHandler(userAgent, apiKey);
            var settings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            ScheduleSettings = RestService.For<IScheduleSettingsApi>(url, settings);
        }
    }
}