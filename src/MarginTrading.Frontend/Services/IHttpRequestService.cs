using System;
using System.Threading.Tasks;
using MarginTrading.Common.Services.Settings;

namespace MarginTrading.Frontend.Services
{
    public interface IHttpRequestService
    {
        Task<TResponse> RequestWithRetriesAsync<TResponse>(object request, string action, bool isLive = true, string controller = "mt");

        Task<TResponse> GetAsync<TResponse>(string path, bool isLive = true, int timeout = 30);

        /// <summary>
        /// Makes a post requests for available backends for client (live/demo) and gets results.
        /// If a backend is not available for client or request fails - <paramref name="defaultResult"/> is returned instead.
        /// </summary>
        Task<(TResponse Demo, TResponse Live)> RequestIfAvailableAsync<TResponse>(object request, string action, Func<TResponse> defaultResult, EnabledMarginTradingTypes enabledMarginTradingTypes, string controller = "mt")
            where TResponse : class;
    }
}