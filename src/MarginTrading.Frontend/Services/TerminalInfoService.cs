using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Settings;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Frontend.Services
{
    /// <summary>
    /// Gets terminal info from settings for terminal ID passed in corresponding request header
    /// </summary>
    public class TerminalInfoService : ITerminalInfoService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TerminalsSettings _settings;

        public TerminalInfoService(IHttpContextAccessor httpContextAccessor,
            TerminalsSettings settings)
        {
            _httpContextAccessor = httpContextAccessor;
            _settings = settings;
        }

        public TerminalInfo Get()
        {
            var context = _httpContextAccessor.HttpContext;

            string terminalId = string.Empty;

            if (context.Request.Headers.ContainsKey(_settings.TerminalIdHeaderName))
            {
                terminalId = context.Request.Headers[_settings.TerminalIdHeaderName].ToString();
            }

            //try get settings for terminal ID passed in header (or for empty string if header is empty)
            if (!_settings.Settings.TryGetValue(terminalId, out var terminalSettings))
            {
                //if terminal ID was not empty but no settings exists for it, we try to get default settings
                if (terminalId != string.Empty)
                {
                    _settings.Settings.TryGetValue(string.Empty, out terminalSettings);
                }
            }

            return new TerminalInfo(terminalId,
                terminalSettings?.DemoEnabled ?? false,
                terminalSettings?.LiveEnabled ?? false);
        }
    }
}