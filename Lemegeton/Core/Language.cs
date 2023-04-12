using System;
using System.Collections.Generic;

namespace Lemegeton.Core
{

    public class Language
    {

        public bool IsDefault { get; set; }
        public string LanguageName { get; set; }

        private Dictionary<string, string> Translations { get; set; }

        public Language()
        {
            LanguageName = "(undefined)";
            Translations = new Dictionary<string, string>();
        }

        internal void AddEntry(string key, string value)
        {
            Translations[key] = value;
        }

        public bool HasTranslationFor(string key)
        {
            return Translations.ContainsKey(key);
        }

        public string Translate(string key, params object[] args)
        {
            if (Translations.ContainsKey(key) == true)
            {
                return String.Format(Translations[key], args);
            }
            return String.Format(key, args);
        }

    }

}
