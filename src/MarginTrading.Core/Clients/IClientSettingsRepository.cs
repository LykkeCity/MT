using System.Threading.Tasks;
using MarginTrading.Core.Assets;

namespace MarginTrading.Core.Clients
{
	public abstract class TraderSettingsBase
	{
		public abstract string GetId();


		public static T CreateDefault<T>() where T : TraderSettingsBase, new()
		{
			if (typeof (T) == typeof (KycProfileSettings))
				return KycProfileSettings.CreateDefault() as T;

			if (typeof(T) == typeof(AppUsageSettings))
				return AppUsageSettings.CreateDefault() as T;

			if (typeof(T) == typeof(AssetPairsInvertedSettings))
				return AssetPairsInvertedSettings.CreateDefault() as T;

			if (typeof(T) == typeof(LastBaseAssetsIos))
				return LastBaseAssetsIos.CreateDefault() as T;

			if (typeof(T) == typeof(LastBaseAssetsOther))
				return LastBaseAssetsOther.CreateDefault() as T;

			if (typeof(T) == typeof(RefundAddressSettings))
				return RefundAddressSettings.CreateDefault() as T;

			if (typeof(T) == typeof(PushNotificationsSettings))
				return PushNotificationsSettings.CreateDefault() as T;

			if (typeof(T) == typeof(MyLykkeSettings))
				return MyLykkeSettings.CreateDefault() as T;

			if (typeof(T) == typeof(BackupSettings))
				return BackupSettings.CreateDefault() as T;

			if (typeof(T) == typeof(SmsSettings))
				return SmsSettings.CreateDefault() as T;

			if (typeof(T) == typeof(HashedPwdSettings))
				return HashedPwdSettings.CreateDefault() as T;

			if (typeof(T) == typeof(CashOutBlockSettings))
				return CashOutBlockSettings.CreateDefault() as T;

			if (typeof(T) == typeof(MarginEnabledSettings))
				return MarginEnabledSettings.CreateDefault() as T;

			return new T();
		}
	}
  

	public class KycProfileSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "KycProfile";
		}

		public bool ShowIdCard { get; set; }
		public bool ShowIdProofOfAddress { get; set; }
		public bool ShowSelfie { get; set; }

		public static KycProfileSettings CreateDefault()
		{
			return new KycProfileSettings
			{
				ShowIdCard = true,
				ShowIdProofOfAddress = true,
				ShowSelfie = true
			};
		}

	}

	public class AppUsageSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "AppUsage";
		}

		public string LastUsedGraphPeriod { get; set; }

		public static AppUsageSettings CreateDefault()
		{
			return new AppUsageSettings
			{
				LastUsedGraphPeriod = GraphPeriod.Hour.Value
			};
		}

	}

	public class AssetPairsInvertedSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "AssetPairsInverted";
		}

		public string[] InvertedAssetIds { get; set; }

		public static AssetPairsInvertedSettings CreateDefault()
		{
			return new AssetPairsInvertedSettings
			{
				InvertedAssetIds = new string[0]
			};
		}

	}

	public class LastBaseAssetsIos : TraderSettingsBase
	{
		public override string GetId()
		{
			return "LastBaseAssetsIos";
		}

		public string[] BaseAssets { get; set; }

		public static LastBaseAssetsIos CreateDefault()
		{
			return new LastBaseAssetsIos
			{
				BaseAssets = new string[0]
			};
		}
	}

	public class LastBaseAssetsOther : TraderSettingsBase
	{
		public override string GetId()
		{
			return "LastBaseAssetsOther";
		}

		public string[] BaseAssets { get; set; }

		public static LastBaseAssetsOther CreateDefault()
		{
			return new LastBaseAssetsOther
			{
				BaseAssets = new string[0]
			};
		}
	}

	public class RefundAddressSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "RefundAddressSettings";
		}

		public string Address { get; set; }
		public int? ValidDays { get; set; }
		public bool? SendAutomatically { get; set; }

		public static RefundAddressSettings CreateDefault()
		{
			return new RefundAddressSettings
			{
				Address = string.Empty,
				ValidDays = LykkeConstants.DefaultRefundTimeoutDays,
				SendAutomatically = false
			};
		}
	}

	public static class RefundAddressSettingsExt
	{
		public static int GetValidInMinutes(this RefundAddressSettings settings, int defaultInMins)
		{
			return /* ToDo: return when will be corrected on client: settings.ValidDays * 24 * 60 ??  */ defaultInMins;
		}

		public static bool RefundCanBeSent(this RefundAddressSettings settings)
		{
			return !string.IsNullOrEmpty(settings.Address); /* &&
				   (settings.SendAutomatically != null && (bool) settings.SendAutomatically ||
					settings.SendAutomatically == null); Todo: Parameter was temporary removed from client. Ignore until will be returned*/
		}
	}

	public class PushNotificationsSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "PushNotificationsSettings";
		}

		public bool Enabled { get; set; }

		public static PushNotificationsSettings CreateDefault()
		{
			return new PushNotificationsSettings
			{
				Enabled = true
			};
		}
	}

	public class MyLykkeSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "MyLykkeSettings";
		}

		public bool? MyLykkeEnabled { get; set; }

		public static MyLykkeSettings CreateDefault()
		{
			return new MyLykkeSettings();
		}
	}

	public class BackupSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "BackupSettings";
		}

		public bool BackupDone { get; set; }

		public static BackupSettings CreateDefault()
		{
			return new BackupSettings();
		}
	}

	//Todo: combine User Settings
	public class SmsSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "SmsSettings";
		}

		public bool UseAlternativeProvider { get; set; }

		public static SmsSettings CreateDefault()
		{
			return new SmsSettings();
		}
	}

	//Todo: combine User Settings
	public class HashedPwdSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "HashedPwdSettings";
		}

		public bool IsPwdHashed { get; set; }

		public static HashedPwdSettings CreateDefault()
		{
			return new HashedPwdSettings();
		}
	}

	public class CashOutBlockSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "CashOutBlockSettings";
		}

		public bool CashOutBlocked { get; set; }
		public bool TradesBlocked { get; set; }

		public static CashOutBlockSettings CreateDefault()
		{
			return new CashOutBlockSettings();
		}
	}

	public class MarginEnabledSettings : TraderSettingsBase
	{
		public override string GetId()
		{
			return "MarginEnabledSettings";
		}

		public bool Enabled { get; set; }

		public static MarginEnabledSettings CreateDefault()
		{
			return new MarginEnabledSettings
			{
				Enabled = true
			};
		}
	}

	public interface IClientSettingsRepository
	{
		Task<T> GetSettings<T>(string traderId) where T : TraderSettingsBase, new();
		Task SetSettings<T>(string traderId, T settings) where T : TraderSettingsBase, new();
		Task DeleteAsync<T>(string traderId) where T : TraderSettingsBase, new();
		Task UpdateKycDocumentSettingOnUpload(string clientId, string modelType);
	}

}
