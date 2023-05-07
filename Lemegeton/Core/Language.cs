using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lemegeton.Core
{

    public class Language
    {

        public enum GlyphRangeEnum
        {
            Undefined,
            ChineseSimplifiedCommon,
            ChineseFull
        }

        public bool IsDefault { get; set; }
        public string LanguageName { get; set; }
        public string FontDownload { get; set; } = null;
        public GlyphRangeEnum GlyphRange { get; set; } = GlyphRangeEnum.Undefined;

        internal ImFontPtr? Font { get; set; } = null;
        internal float Coverage { get; set; }

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

        internal void CalculateCoverage(Language master)
        {
            Coverage = (float)Translations.Count / (float)master.Translations.Count;
        }

    }

}
