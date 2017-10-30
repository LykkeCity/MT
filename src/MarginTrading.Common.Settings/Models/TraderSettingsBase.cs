namespace MarginTrading.Common.Settings.Models
{
    public abstract class TraderSettingsBase
    {
        public abstract string GetId();


        public static T CreateDefault<T>() where T : TraderSettingsBase, new()
        {
            if (typeof(T) == typeof(PushNotificationsSettings))
                return PushNotificationsSettings.CreateDefault() as T;

            if (typeof(T) == typeof(MarginEnabledSettings))
                return MarginEnabledSettings.CreateDefault() as T;

            return new T();
        }
    }
}