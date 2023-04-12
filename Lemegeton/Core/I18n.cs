using System.Collections.Generic;

namespace Lemegeton.Core
{

    internal static class I18n
    {

        internal static Dictionary<string, Language> RegisteredLanguages = new Dictionary<string, Language>();

        internal static Language DefaultLanguage = null;
        internal static Language CurrentLanguage = null;

        internal static void AddLanguage(Language ld)
        {
            if (ld.IsDefault == true)
            {
                DefaultLanguage = ld;
            }
            if (RegisteredLanguages.ContainsKey(ld.LanguageName) == true)
            {
                string basename = ld.LanguageName;
                for (int i = 2; ; i++)
                {
                    string curname = basename + " #" + i;
                    if (RegisteredLanguages.ContainsKey(curname) == true)
                    {
                        continue;
                    }
                    ld.LanguageName = curname;
                    RegisteredLanguages[curname] = ld;
                    break;
                }
            }
            else
            {
                RegisteredLanguages[ld.LanguageName] = ld;
            }
            if (CurrentLanguage == null)
            {
                CurrentLanguage = ld;
            }
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
                CurrentLanguage = DefaultLanguage;
                return true;
            }
            else
            {
                if (RegisteredLanguages.ContainsKey(langname) == true)
                {
                    CurrentLanguage = RegisteredLanguages[langname];
                    return true;
                }
                else
                {
                    CurrentLanguage = DefaultLanguage;
                    return false;
                }
            }
        }

    }

}
