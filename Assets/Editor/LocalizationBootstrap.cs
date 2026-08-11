#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Build;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

// One-off (but safely re-runnable) setup tool: creates the Locale assets, the
// "Menu" String Table Collection with its keys, and registers the Localization
// Settings + scripting define needed for LocalizationManager to use the real
// Unity Localization package instead of only its hardcoded fallback strings.
public static class LocalizationBootstrap
{
    private const string LocalesFolder = "Assets/Localization/Locales";
    private const string TablesFolder = "Assets/Localization/Tables";
    private const string TableCollectionName = "Menu";

    private static readonly string[,] Keys =
    {
        { "MainMenu.PlayButton", "Play game", "Játék indítása" },
        { "MainMenu.CharactersButton", "Characters", "Karakterek" },
        { "MainMenu.SettingsButton", "Settings", "Beállítások" },
        { "MainMenu.CreditsButton", "Credits", "Stáblista" },
        { "MainMenu.ExitButton", "Exit", "Kilépés" },
        { "MainMenu.OpeningQuote",
          "“In the bleak mid-winter\nFrosty wind made moan\nEarth stood hard as iron\nWater like a stone;\nSnow had fallen, snow on snow,\nSnow on snow,\nIn the bleak mid-winter\nLong ago.” - Christina Rossetti",
          "„A zord télközépen\nFagyos szél jajgatott,\nVasként állt a kemény föld,\nKővé fagyott a víz;\nHó hullott, hóra hó,\nHóra hó,\nA zord télközépen,\nRéges-régen.” - Christina Rossetti" },

        { "Characters.CloseButton", "Close", "Bezárás" },
        { "Characters.NarrationButton", "Narration", "Narráció" },
        { "Characters.BackButton", "Back", "Vissza" },

        { "Credits.Title", "CREDITS", "STÁBLISTA" },
        { "Credits.ProgrammingTitle", "Programming", "Programozás" },
        { "Credits.ArtLabel", "Art & Visual Assets", "Művészet és vizuális elemek" },
        { "Credits.PortraitsLabel", "Portraits & Programming", "Portrék és programozás" },
        { "Credits.UIDesignLabel", "UI Design", "Felület dizájn" },
        { "Credits.WritingLabel", "Writing & Narration", "Írás és narráció" },
        { "Credits.AudioLabel", "Audio Engineering", "Hangmérnöki munka" },
        { "Credits.BackButton", "Back", "Vissza" },
        { "Credits.Name.Bajnok", "David Bajnok", "Bajnok David" },
        { "Credits.Name.Galusz", "Marci Galusz", "Galusz Marci" },
        { "Credits.Name.Barta", "David Barta", "Barta David" },
        { "Credits.Name.NagyBorus", "Levente Nagy Borús", "Nagy Borús Levente" },
        { "Credits.Name.Benda", "Daniel Benda", "Benda Daniel" },
        { "Credits.Name.Boros", "Aron Boros", "Boros Aron" },

        { "Settings.Title", "Settings", "Beállítások" },
        { "Settings.LanguageLabel", "Language", "Nyelv" },
        { "Settings.LanguageButton.EnglishHungarian", "   <b>English</b> / Hungarian", "Angol / <b>Magyar</b>" },
        { "Settings.SubtitlesLabel", "Subtitles", "Feliratok" },
        { "Settings.SubtitlesOn", "<b>ON</b> / OFF", "<b>BE</b> / KI" },
        { "Settings.SubtitlesOff", "ON / <b>OFF</b>", "BE / <b>KI</b>" },
        { "Settings.SubtitlesLanguageLabel", "Subtitles Language", "Felirat nyelve" },
        { "Settings.SubtitlesLanguageButton.EnglishHungarian", "    <b>English</b> / Hungarian", "<b>Angol</b> / Magyar" },
        { "Settings.SubtitlesLanguageButton.HungarianEnglish", "   English / <b>Hungarian</b>", "Angol / <b>Magyar</b>" },
        { "Settings.BackButton", "Back", "Vissza" },

        { "GameScene.ExitConfirmation.Message", "Leave to the main menu?", "Kilépsz a főmenübe?" },
        { "GameScene.ExitConfirmation.YesButton", "Yes", "Igen" },
        { "GameScene.ExitConfirmation.NoButton", "No", "Nem" },
        { "GameScene.Opening.Caption", "1943, 11th of January\nDon River Line, Voronezh Region, USSR\n−35 °C", "1943. január 11.\nDon-kanyar, Voronyézsi terület, Szovjetunió\n−35 °C" },
    };

    [MenuItem("Tools/Localization/Bootstrap Or Update")]
    public static void Run()
    {
        Directory.CreateDirectory(LocalesFolder);
        Directory.CreateDirectory(TablesFolder);
        AssetDatabase.Refresh();

        AddressableAssetSettingsDefaultObject.GetSettings(true);

        Locale en = GetOrCreateLocale("en", "English (en)");
        Locale hu = GetOrCreateLocale("hu", "Hungarian (hu)");

        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableCollectionName);
        if (collection == null)
        {
            collection = LocalizationEditorSettings.CreateStringTableCollection(
                TableCollectionName, TablesFolder, new List<Locale> { en, hu });
        }

        StringTable enTable = collection.GetTable(en.Identifier) as StringTable;
        StringTable huTable = collection.GetTable(hu.Identifier) as StringTable;

        for (int i = 0; i < Keys.GetLength(0); i++)
        {
            string key = Keys[i, 0];
            string enText = Keys[i, 1];
            string huText = Keys[i, 2];

            if (!collection.SharedData.Contains(key))
            {
                collection.SharedData.AddKey(key);
            }

            enTable?.AddEntry(key, enText);
            huTable?.AddEntry(key, huText);
        }

        EditorUtility.SetDirty(collection.SharedData);
        if (enTable != null) EditorUtility.SetDirty(enTable);
        if (huTable != null) EditorUtility.SetDirty(huTable);

        LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Default Localization Settings";
            AssetDatabase.CreateAsset(settings, "Assets/Localization/Default Localization Settings.asset");
            AssetDatabase.SaveAssets();
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        AddDefine(NamedBuildTarget.Standalone, "UNITY_LOCALIZATION");

        NamedBuildTarget activeTarget = NamedBuildTarget.FromBuildTargetGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        if (activeTarget != NamedBuildTarget.Standalone)
        {
            AddDefine(activeTarget, "UNITY_LOCALIZATION");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("LocalizationBootstrap: DONE");
    }

    private static Locale GetOrCreateLocale(string code, string displayName)
    {
        foreach (var locale in LocalizationEditorSettings.GetLocales())
        {
            if (locale.Identifier.Code == code)
            {
                return locale;
            }
        }

        Locale locale2 = Locale.CreateLocale(code);
        locale2.name = displayName;
        AssetDatabase.CreateAsset(locale2, $"{LocalesFolder}/{code}.asset");
        LocalizationEditorSettings.AddLocale(locale2);
        return locale2;
    }

    private static void AddDefine(NamedBuildTarget target, string define)
    {
        string existing = PlayerSettings.GetScriptingDefineSymbols(target);
        var defines = new List<string>(existing.Split(';'));
        if (defines.Contains(define))
        {
            return;
        }

        defines.Add(define);
        PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
    }
}
#endif
