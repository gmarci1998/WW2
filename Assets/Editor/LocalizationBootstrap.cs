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

        { "MainMenu.OneLifeButton", "One Life", "Egy élet" },
        { "OneLife.Intro.Title", "One life.", "Egy élet." },
        { "OneLife.Contains.Header", "What one life contains", "Mit tartalmaz egy élet" },
        { "OneLife.ContinueButton", "Continue", "Tovább" },

        { "OneLife.Contains.Row1.Number", "80", "80" },
        { "OneLife.Contains.Row1.Description", "About 80 birthdays.", "Körülbelül 80 születésnap." },
        { "OneLife.Contains.Row2.Number", "960", "960" },
        { "OneLife.Contains.Row2.Description", "About 960 months.", "Körülbelül 960 hónap." },
        { "OneLife.Contains.Row3.Number", "1,000", "1000" },
        { "OneLife.Contains.Row3.Description", "About 1,000 full moons. A thousand nights when the moon returned, whether anyone noticed or not.", "Körülbelül 1000 telihold. Ezer éjszaka, amikor visszatért a hold, akár észrevette valaki, akár nem." },
        { "OneLife.Contains.Row4.Number", "4,000", "4000" },
        { "OneLife.Contains.Row4.Description", "About 4,000 weekends. The days you hoped would keep coming forever.", "Körülbelül 4000 hétvége. A napok, amikről azt reméltük, örökké jönnek majd." },
        { "OneLife.Contains.Row5.Number", "7,000", "7000" },
        { "OneLife.Contains.Row5.Description", "About 7,000 songs heard more than once. Songs from childhood. Songs from kitchens, radios, dances, funerals, and barracks.", "Körülbelül 7000 dal, amit egynél többször hallottunk. Dalok a gyerekkorból. Dalok konyhákból, rádiókból, táncmulatságokból, temetésekről és laktanyákból." },
        { "OneLife.Contains.Row6.Number", "10,000", "10 000" },
        { "OneLife.Contains.Row6.Description", "Around 10,000 people you meet during your days on Earth. Friends, strangers, teachers, neighbors, colleagues, enemies and lovers.", "Körülbelül 10 000 ember, akivel találkozunk földi napjaink során. Barátok, idegenek, tanárok, szomszédok, kollégák, ellenségek és szerelmek." },
        { "OneLife.Contains.Row7.Number", "15,000", "15 000" },
        { "OneLife.Contains.Row7.Description", "About 15,000 conversations. Some forgotten immediately. Some replayed in your head for decades.", "Körülbelül 15 000 beszélgetés. Némelyik azonnal feledésbe merül. Némelyik évtizedekig visszhangzik a fejünkben." },
        { "OneLife.Contains.Row8.Number", "20,000", "20 000" },
        { "OneLife.Contains.Row8.Description", "About 20,000 walks from one place to another. To school. To work. To the shop. To someone waiting.", "Körülbelül 20 000 séta egyik helyről a másikra. Iskolába. Munkába. A boltba. Valakihez, aki vár." },
        { "OneLife.Contains.Row9.Number", "29,000", "29 000" },
        { "OneLife.Contains.Row9.Description", "About 29,000 dinners. Meals at the kitchen table. Quick bites between errands. Celebrations at restaurants. Something greasy after a night out with friends. A nice, hearty meal with someone you loved.", "Körülbelül 29 000 vacsora. Étkezések a konyhaasztalnál. Gyors falatok ügyintézés közben. Ünneplések éttermekben. Valami zsíros egy barátokkal töltött éjszaka után. Egy jó, laktató étel valakivel, akit szerettünk." },

        { "OneLife.Bars.Bar1.Number", "30", "30" },
        { "OneLife.Bars.Bar1.Caption", "30 dead is a classroom. The number of children who once sat around you while you learned to read, whispered jokes, and waited for the bell.", "30 halott egy osztályterem. Annyi gyerek, ahányan egykor körülötted ültek, míg olvasni tanultatok, vicceket sugdostatok, és a csengőre vártatok." },
        { "OneLife.Bars.Bar2.Number", "80", "80" },
        { "OneLife.Bars.Bar2.Caption", "80 dead is a crowded city bus. The people pressed around you on a rainy morning, all going somewhere ordinary.", "80 halott egy zsúfolt városi busz. Az emberek, akik egy esős reggelen köréd préselődtek, mindannyian valami hétköznapi cél felé tartva." },
        { "OneLife.Bars.Bar3.Number", "250", "250" },
        { "OneLife.Bars.Bar3.Caption", "250 dead is a small cinema audience. The room you once shared with strangers while watching the film you loved as a child.", "250 halott egy kisebb mozinéző közönség. A terem, amelyen egykor idegenekkel osztoztál, miközben gyerekkorod kedvenc filmjét nézted." },
        { "OneLife.Bars.Bar4.Number", "300", "300" },
        { "OneLife.Bars.Bar4.Caption", "300 dead is a full passenger plane. Everyone who boarded, put bags overhead, fastened seatbelts, and expected to land.", "300 halott egy tele utasszállító repülőgép. Mindenki, aki felszállt, feltette a csomagját, becsatolta az övét, és arra számított, hogy landolni fog." },
        { "OneLife.Bars.Bar5.Number", "1,000", "1000" },
        { "OneLife.Bars.Bar5.Caption", "1,000 dead is an entire school. Every student in the hallway. Every teacher behind a classroom door. Everyone inside it.", "1000 halott egy egész iskola. Minden diák a folyosón. Minden tanár egy tanterem ajtaja mögött. Mindenki, aki bent van." },
        { "OneLife.Bars.Bar6.Number", "5,000", "5000" },
        { "OneLife.Bars.Bar6.Caption", "5,000 dead is a concert crowd. The mass of strangers you once stood among, shoulder to shoulder, singing the same chorus.", "5000 halott egy koncertközönség. Az idegenek tömege, akik közt egykor álltál, vállt vállnak vetve, ugyanazt a refrént énekelve." },
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
