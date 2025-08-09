using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lemegeton.Core
{

    public abstract class Language
    {

        public enum GlyphRangeEnum
        {
            Undefined,
            ChineseSimplifiedCommon,
            ChineseFull
        }

        public abstract bool IsDefault { get; }
        public abstract string LanguageName { get; }
        public abstract bool FontDownloadNecessary { get; }
        public abstract string FontDownload { get; }
        public abstract GlyphRangeEnum GlyphRange { get; }

        internal State _state { get; set; } = null;
        internal ImFontPtr? Font { get; set; } = null;
        internal float Coverage { get; set; }

        private Dictionary<string, string> Translations { get; set; }

        public Language(State st)
        {
            Translations = new Dictionary<string, string>();
            _state = st;
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

        internal void CalculateCoverage(Language master)
        {
            Coverage = (float)Translations.Count / (float)master.Translations.Count;
        }

    }

}
