using System;
using System.Collections.Generic;
namespace MarginTrading.Frontend.Models
{
    public class TranslationRequestModel
    {
        public string Language { get; set; }
        public Dictionary<string, string> Translations { get; set; }
    }

    public class TranslationsResponse
    {
        public Dictionary<string, string> Translations { get; set; }
    }
}
