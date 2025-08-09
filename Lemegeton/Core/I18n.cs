﻿using Dalamud.Bindings.ImGui;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;

namespace Lemegeton.Core
{

    internal static class I18n
    {

        internal delegate void FontDownloadRequest(Language lang);
        internal static event FontDownloadRequest OnFontDownload;

        internal static Dictionary<string, Language> RegisteredLanguages = new Dictionary<string, Language>();
        internal static Language DefaultLanguage = null;
        private static Language _CurrentLanguage = null;
        internal static Language CurrentLanguage
        {
            get
            {
                return _CurrentLanguage;
            }
            set
            {
                if (value != _CurrentLanguage)
                {
                    _CurrentLanguage = value;
                    if (_CurrentLanguage != null && _CurrentLanguage.Font == null)
                    {
                        if (_CurrentLanguage.FontDownload != null && _CurrentLanguage.FontDownloadNecessary == true) 
                        {
                            OnFontDownload?.Invoke(_CurrentLanguage);
                        }
                    }
                }
            }
        }

        internal static void AddLanguage(Language ld)
        {
            if (ld.IsDefault == true)
            {
                DefaultLanguage = ld;
            }
            RegisteredLanguages[ld.LanguageName] = ld;
            if (_CurrentLanguage == null)
            {
                _CurrentLanguage = ld;
            }
        }

        internal static ImFontPtr? GetFont()
        {
            return CurrentLanguage != null ? CurrentLanguage.Font : DefaultLanguage.Font;
        }

        internal static string Translate(string key, params object[] args)
        {
            if (CurrentLanguage != null && CurrentLanguage.HasTranslationFor(key) == true)
            {
                return CurrentLanguage.Translate(key, args);
            }
            return DefaultLanguage.Translate(key, args);
        }

        internal static bool ChangeLanguage(string langname)
        {
            if (langname == null)
            {
                _CurrentLanguage = null;
                CurrentLanguage = DefaultLanguage;
                return true;
            }
            else
            {
                if (RegisteredLanguages.ContainsKey(langname) == true)
                {
                    _CurrentLanguage = null;
                    CurrentLanguage = RegisteredLanguages[langname];
                    return true;
                }
                else
                {
                    _CurrentLanguage = null;
                    CurrentLanguage = DefaultLanguage;
                    return false;
                }
            }
        }

    }

}
