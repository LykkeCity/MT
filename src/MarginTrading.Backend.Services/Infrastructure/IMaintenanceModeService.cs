namespace MarginTrading.Backend.Services.Infrastructure
{
    public interface IMaintenanceModeService
    {
        bool CheckIsEnabled();

        void SetMode(bool isEnabled);
    }
}
