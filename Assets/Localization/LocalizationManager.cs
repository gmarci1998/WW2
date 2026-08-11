using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class LocalizationManager
{
    public const string MenuLanguagePrefKey = "NarrationLanguage";
    public const string StringTableName = "Menu";
    private const string DefaultLanguage = "English";

    public static string CurrentLanguage { get; private set; }
    public static bool IsInitialized { get; private set; }

    static LocalizationManager()
    {
        CurrentLanguage = DefaultLanguage;
    }

    public static event Action<string> LanguageChanged;

    private static readonly Dictionary<string, string> LanguageToLocaleCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "English", "en" },
        { "Hungarian", "hu" }
    };

    // Last-resort fallback if the String Table is missing or a key isn't found there.
    private static readonly Dictionary<string, Dictionary<string, string>> FallbackTranslations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
    {
        { "Settings.Title", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "Settings" }, { "Hungarian", "Beállítások" } } },
        { "Settings.LanguageLabel", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "Language" }, { "Hungarian", "Nyelv" } } },
        { "Settings.LanguageButton.EnglishHungarian", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "   <b>English</b> / Hungarian" }, { "Hungarian", "Angol / <b>Magyar</b>" } } },
        { "Settings.SubtitlesLabel", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "Subtitles" }, { "Hungarian", "Feliratok" } } },
        { "Settings.SubtitlesOn", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "<b>ON</b> / OFF" }, { "Hungarian", "<b>BE</b> / KI" } } },
        { "Settings.SubtitlesOff", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "ON / <b>OFF</b>" }, { "Hungarian", "BE / <b>KI</b>" } } },
        { "Settings.SubtitlesLanguageLabel", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "Subtitles Language" }, { "Hungarian", "Felirat nyelve" } } },
        { "Settings.SubtitlesLanguageButton.EnglishHungarian", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "    <b>English</b> / Hungarian" }, { "Hungarian", "<b>Angol</b> / Magyar" } } },
        { "Settings.SubtitlesLanguageButton.HungarianEnglish", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "   English / <b>Hungarian</b>" }, { "Hungarian", "Angol / <b>Magyar</b>" } } },
        { "Settings.BackButton", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "English", "Back" }, { "Hungarian", "Vissza" } } }
    };

    public static void Initialize()
    {
        CurrentLanguage = PlayerPrefs.GetString(MenuLanguagePrefKey, DefaultLanguage);
        if (string.IsNullOrEmpty(CurrentLanguage))
        {
            CurrentLanguage = DefaultLanguage;
        }

        SetLanguage(CurrentLanguage, force: true);
        IsInitialized = true;
    }

    public static void SetLanguage(string language, bool force = false)
    {
        language = NormalizeLanguage(language);
        if (!force && string.Equals(language, CurrentLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentLanguage = language;
        PlayerPrefs.SetString(MenuLanguagePrefKey, CurrentLanguage);
        PlayerPrefs.Save();

        ApplyLocale(language);

        if (LanguageChanged != null)
        {
            LanguageChanged(CurrentLanguage);
        }
    }

    public static string GetText(string key, string fallback)
    {
        var localized = GetLocalizedText(key);
        if (!string.IsNullOrEmpty(localized))
        {
            return localized;
        }

        return GetFallbackText(key, fallback);
    }

    private static string NormalizeLanguage(string language)
    {
        if (string.Equals(language, "Hungarian", StringComparison.OrdinalIgnoreCase))
        {
            return "Hungarian";
        }

        return "English";
    }

    private static string GetFallbackText(string key, string fallback)
    {
        Dictionary<string, string> map;
        string translated;

        if (FallbackTranslations.TryGetValue(key, out map) && map.TryGetValue(CurrentLanguage, out translated))
        {
            return translated;
        }

        return fallback;
    }

    private static string GetLocalizedText(string key)
    {
        if (LocalizationSettings.StringDatabase == null)
        {
            return null;
        }

        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            LocalizationSettings.InitializationOperation.WaitForCompletion();
        }

        AsyncOperationHandle<string> handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(StringTableName, key);
        if (handle.IsValid())
        {
            handle.WaitForCompletion();
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
        }

        return null;
    }

    private static void ApplyLocale(string language)
    {
        if (LocalizationSettings.AvailableLocales == null || LocalizationSettings.AvailableLocales.Locales == null)
        {
            return;
        }

        if (!LanguageToLocaleCode.TryGetValue(language, out string localeCode))
        {
            localeCode = "en";
        }

        Locale selectedLocale = null;
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code.Equals(localeCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(locale.Identifier.CultureInfo?.TwoLetterISOLanguageName, localeCode, StringComparison.OrdinalIgnoreCase))
            {
                selectedLocale = locale;
                break;
            }
        }

        if (selectedLocale != null)
        {
            LocalizationSettings.SelectedLocale = selectedLocale;
        }
    }
}
