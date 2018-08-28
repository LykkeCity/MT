using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class CqrsContextNamesSettings
    {
        [Optional] public string AccountsManagement { get; set; } = nameof(AccountsManagement);

        [Optional] public string TradingEngine { get; set; } = nameof(TradingEngine);

        [Optional] public string SettingsService { get; set; } = nameof(SettingsService);
    }
}