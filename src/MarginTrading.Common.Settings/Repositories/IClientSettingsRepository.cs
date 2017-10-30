using System.Threading.Tasks;
using MarginTrading.Common.Settings.Models;

namespace MarginTrading.Common.Settings.Repositories
{
	public interface IClientSettingsRepository
	{
		Task<T> GetSettings<T>(string traderId) where T : TraderSettingsBase, new();
		Task SetSettings<T>(string traderId, T settings) where T : TraderSettingsBase, new();
	}

}
