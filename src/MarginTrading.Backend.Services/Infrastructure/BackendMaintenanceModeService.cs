namespace MarginTrading.Backend.Services.Infrastructure
{
    public class BackendMaintenanceModeService : IMaintenanceModeService
    {
        private static bool IsEnabled { get; set; }

        public bool CheckIsEnabled()
        {
            return IsEnabled;
        }

        public void SetMode(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }
}