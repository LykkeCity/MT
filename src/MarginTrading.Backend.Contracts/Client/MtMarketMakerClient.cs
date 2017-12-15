using Refit;

namespace MarginTrading.Backend.Contracts.Client
{
    internal class MtBackendClient : IMtBackendClient
    {
        public IDayOffExclusionsApi DayOffExclusions { get; }
        public IScheduleSettingsApi ScheduleSettings { get; }

        public MtBackendClient(string url, string apiKey, string userAgent)
        {
            var httpMessageHandler = new MtBackendHttpClientHandler(userAgent, apiKey);
            var settings = new RefitSettings {HttpMessageHandlerFactory = () => httpMessageHandler};
            DayOffExclusions = RestService.For<IDayOffExclusionsApi>(url, settings);
            ScheduleSettings = RestService.For<IScheduleSettingsApi>(url, settings);
        }
    }
}