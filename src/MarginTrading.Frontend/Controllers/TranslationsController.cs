using System.Collections.Generic;
using System.Linq;
using MarginTrading.Frontend.Models;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
	[Route("api/translations")]
	public class TranslationsController : Controller
	{
		[Route("")]
		[HttpPost]
		public TranslationsResponse GetTranslations([FromBody]TranslationRequestModel model)
		{
			var existingTranslations = new List<Translation>
			{
				new Translation{Languagge = "th", Key = "tab.wallets", Value = "กระเป๋าสตางค์"},
				new Translation{Languagge = "th", Key = "tab.trading", Value = "การค้าขาย"},
				new Translation{Languagge = "th", Key = "tab.transfer", Value = "โอน"},
				new Translation{Languagge = "th", Key = "tab.history", Value = "ประวัติศาสตร์"},
				new Translation{Languagge = "th", Key = "tab.settings", Value = "การตั้งค่า"},
				new Translation{Languagge = "th", Key = "tab.wallets.trading", Value = "กระเป๋าสตางค์"},
				new Translation{Languagge = "th", Key = "tab.wallets.margin", Value = "กระเป๋าสตางค์"},
				new Translation{Languagge = "th", Key = "tab.wallets.private", Value = "กระเป๋าเงินส่วนตัว"}
			};

			var translations = existingTranslations.Where(item => model.Translations.Keys.Contains(item.Key)).ToList();

			foreach (var translation in translations)
			{
				model.Translations[translation.Key] = translation.Value;
			}

			return new TranslationsResponse
			{
				Translations = model.Translations
			};
		}
	}
}
