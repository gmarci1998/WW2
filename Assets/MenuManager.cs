using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public CanvasGroup mainMenu;
    public CanvasGroup charactersMenu;
    public CanvasGroup creditsMenu;
    public CanvasGroup openingScene;
    public CanvasGroup settingsScene;
    public CanvasGroup oneLifeMenu;
    public string GameScene = "GameScene";
    public string language;
    public bool subtitlesEnabled;
    public string subtitlesLanguage;

    [Header("Main Menu UI")]
    [SerializeField] private TextMeshProUGUI DONTitleText;
    [SerializeField] private TextMeshProUGUI playButtonText;
    [SerializeField] private TextMeshProUGUI charactersButtonText;
    [SerializeField] private TextMeshProUGUI settingsButtonText;
    [SerializeField] private TextMeshProUGUI creditsButtonText;
    [SerializeField] private TextMeshProUGUI exitButtonText;

    [Header("Opening Sequence UI")]
    [SerializeField] private TextMeshProUGUI openingQuoteText;

    [Header("One Life UI")]
    [SerializeField] private TextMeshProUGUI oneLifeButtonText;
    [SerializeField] private TextMeshProUGUI oneLifeIntroTitleText;
    [SerializeField] private TextMeshProUGUI oneLifeHeaderText;
    [SerializeField] private TextMeshProUGUI oneLifeContinueButtonText;


    [Header("Characters Menu UI")]
    [SerializeField] private TextMeshProUGUI closeButtonText;
    [SerializeField] private TextMeshProUGUI narrationButtonText;
    [SerializeField] private TextMeshProUGUI backButtonText;

    [Header("Credits Menu UI")]
    [SerializeField] private TextMeshProUGUI creditsTitleText;
    [SerializeField] private TextMeshProUGUI programmingTitleText;
    [SerializeField] private TextMeshProUGUI artLabelText;
    [SerializeField] private TextMeshProUGUI portraitsLabelText;
    [SerializeField] private TextMeshProUGUI uiDesignLabelText;
    [SerializeField] private TextMeshProUGUI writingLabelText;
    [SerializeField] private TextMeshProUGUI audioLabelText;
    [SerializeField] private TextMeshProUGUI charactersExitButtonText;
    [SerializeField] private TextMeshProUGUI creditsNameBajnokText;
    [SerializeField] private TextMeshProUGUI creditsNameGaluszText;
    [SerializeField] private TextMeshProUGUI creditsNameBartaText;
    [SerializeField] private TextMeshProUGUI creditsNameNagyBorusText;
    [SerializeField] private TextMeshProUGUI creditsNameBendaText;
    [SerializeField] private TextMeshProUGUI creditsNameBorosText;
    [SerializeField] private TextMeshProUGUI hungarianNarrationLabelText;
    [SerializeField] private TextMeshProUGUI creditsNameNagySandorText;
    [SerializeField] private TextMeshProUGUI englishNarrationLabelText;
    [SerializeField] private TextMeshProUGUI creditsNameHulseText;


    [Header("Settings Menu UI")]
    [SerializeField] private TextMeshProUGUI settingsTitleText;
    [SerializeField] private TextMeshProUGUI languageLabelText;
    [SerializeField] private TextMeshProUGUI languageButtonText;
    [SerializeField] private TextMeshProUGUI subtitlesLabelText;
    [SerializeField] private TextMeshProUGUI subtitlesButtonText;
    [SerializeField] private TextMeshProUGUI subtitlesLanguageLabelText;
    [SerializeField] private TextMeshProUGUI subtitlesLanguageButtonText;
    [SerializeField] private TextMeshProUGUI settingsBackButtonText;

    [SerializeField] private Button subtitlesLanguageLabel;
    [SerializeField] private Button subtitlesLanguageButton;


    public float fadeTime = 0.4f;

    public void OpenCharacters()
    {
        StartCoroutine(SwitchMenu(mainMenu, charactersMenu));
    }

    public void OpenSettings()
    {
        StartCoroutine(SwitchMenu(mainMenu, settingsScene));
    }

    public void OpenCredits()
    {
        StartCoroutine(SwitchMenu(mainMenu, creditsMenu));
    }

    public void OpenOneLife()
    {
        StartCoroutine(SwitchMenu(mainMenu, oneLifeMenu));
    }

    public void ContinueFromOneLifeToCredits()
    {
        oneLifeMenu.interactable = false;
        oneLifeMenu.blocksRaycasts = false;
        oneLifeMenu.gameObject.SetActive(false);

        creditsMenu.gameObject.SetActive(true);
        creditsMenu.alpha = 1f;
        creditsMenu.interactable = true;
        creditsMenu.blocksRaycasts = true;
    }

    public void BackFromCharacters()
    {
        StartCoroutine(SwitchMenu(charactersMenu, mainMenu));
    }

    public void BackFromSettings()
    {
        StartCoroutine(SwitchMenu(settingsScene, mainMenu));
    }

    public void BackFromCredits()
    {
        StartCoroutine(SwitchMenu(creditsMenu, mainMenu));
    }

    public void PlayGame()
    {
        StartCoroutine(PlayGameSequence());
    }

    IEnumerator PlayGameSequence()
    {
        yield return StartCoroutine(SwitchMenu(mainMenu, openingScene));
        yield return new WaitForSeconds(9f);
        SceneManager.LoadScene(GameScene);
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    IEnumerator SwitchMenu(CanvasGroup from, CanvasGroup to)
    {
        yield return StartCoroutine(Fade(from, 1f, 0f));
        from.interactable = false;
        from.blocksRaycasts = false;
        from.gameObject.SetActive(false);

        to.gameObject.SetActive(true);
        yield return new WaitForEndOfFrame();
        to.alpha = 0f;
        yield return StartCoroutine(Fade(to, 0f, 1f));
        to.interactable = true;
        to.blocksRaycasts = true;
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to)
    {
        float t = 0f;
        cg.alpha = from;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }

        cg.alpha = to;
    }



    [SerializeField] private SoldierData[] HungarianSoldiers;
    [SerializeField] private SoldierData[] RussianSoldiers;
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;
    [SerializeField] private Button button4;
    [SerializeField] private Button button5;
    [SerializeField] private Button button6;
    [SerializeField] private Button button7;
    [SerializeField] private Button button8;

    [Header("IDEIGLENES - teszteleshez")]
    [Tooltip("Ha be van kapcsolva, a karakter menuben minden karakter kattinthato, fuggetlenul attol, hogy vegighallgattad-e mar a narraciojat. Teszteles utan kapcsold ki.")]
    [SerializeField] private bool unlockAllCharacters = false;

    [Header("Characters Menu Locked Visual")]
    [SerializeField] private float lockedButtonAlpha = 0.25f;
    [SerializeField] private Color buttonShadowColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Vector2 buttonShadowDistance = new Vector2(5f, -5f);

    [System.Serializable]
    public class SoldierSaveData 
    {
        public string Name;
        public bool isOpened;
    }

    [System.Serializable]
    public class SaveWrapper 
    {
        public List<SoldierSaveData> Soldiers;
        public string NarrationLanguage;
        public bool SubtitlesEnabled;
        public string SubtitlesLanguage;
    }

    void Awake()
    {
        StartCoroutine(CheckOneLifeOnAwake());
    }

    IEnumerator CheckOneLifeOnAwake()
    {
        yield return null;

        if (GameManager.Instance != null && mainMenu != null && oneLifeMenu != null)
        {
            bool shouldShowOneLife = GameManager.Instance.showOneLifeOnGameEnd;
            GameManager.Instance.showOneLifeOnGameEnd = false;

            if (shouldShowOneLife)
            {
                // Főmenü elrejtése
                mainMenu.alpha = 0f;
                mainMenu.gameObject.SetActive(false);

                StartCoroutine(OpenOneLifeSafe());
                yield break;
            }
        }
    }

    IEnumerator OpenOneLifeSafe()
    {
        yield return new WaitForEndOfFrame(); // Biztos stabilitás

        // One Life aktiválása
        oneLifeMenu.gameObject.SetActive(true);
        oneLifeMenu.alpha = 0f;

        // Gyors fade-in one life-ra
        StartCoroutine(Fade(oneLifeMenu, 0f, 1f));
        oneLifeMenu.interactable = true;
        oneLifeMenu.blocksRaycasts = true;

        // Inicializálás
        LoadSoldiersFromFile();
        UnlockCursor();
    }

    void Start()  
    {
        LoadSoldiersFromFile();
        LocalizationManager.Initialize();
        LocalizationManager.SetLanguage(language);
        language = LocalizationManager.CurrentLanguage;
        Debug.Log(language);
        ChangeLanguage();
        UnlockCursor();
        HideSubtitleLanguageOption();
    }

    
    IEnumerator OpenCreditsImmediate()
    {
        yield return null; 
        if (creditsMenu != null)
        {
            creditsMenu.gameObject.SetActive(true);
            StartCoroutine(SwitchMenu(null, creditsMenu));
        }
        //LoadSoldiersFromFile();

        UnlockCursor();
    }

    /*
[SerializeField] private TextMeshProUGUI settingsTitleText;
    [SerializeField] private TextMeshProUGUI languageLabelText;
    [SerializeField] private TextMeshProUGUI languageButtonText;
    [SerializeField] private TextMeshProUGUI subtitlesLabelText;
    [SerializeField] private TextMeshProUGUI subtitlesButtonText;
    [SerializeField] private TextMeshProUGUI subtitlesLanguageLabelText;
    [SerializeField] private TextMeshProUGUI subtitlesLanguageButtonText;
    [SerializeField] private TextMeshProUGUI settingsBackButtonText;
    */

    public void ChangeLanguage()
    {
        if (playButtonText != null) playButtonText.text = LocalizationManager.GetText("MainMenu.PlayButton", playButtonText.text);
        if (charactersButtonText != null) charactersButtonText.text = LocalizationManager.GetText("MainMenu.CharactersButton", charactersButtonText.text);
        if (settingsButtonText != null) settingsButtonText.text = LocalizationManager.GetText("MainMenu.SettingsButton", settingsButtonText.text);
        if (creditsButtonText != null) creditsButtonText.text = LocalizationManager.GetText("MainMenu.CreditsButton", creditsButtonText.text);
        if (exitButtonText != null) exitButtonText.text = LocalizationManager.GetText("MainMenu.ExitButton", exitButtonText.text);
        if (openingQuoteText != null) openingQuoteText.text = LocalizationManager.GetText("MainMenu.OpeningQuote", openingQuoteText.text);

        if (oneLifeButtonText != null) oneLifeButtonText.text = LocalizationManager.GetText("MainMenu.OneLifeButton", oneLifeButtonText.text);
        if (oneLifeIntroTitleText != null) oneLifeIntroTitleText.text = LocalizationManager.GetText("OneLife.Intro.Title", oneLifeIntroTitleText.text);
        if (oneLifeHeaderText != null) oneLifeHeaderText.text = LocalizationManager.GetText("OneLife.Contains.Header", oneLifeHeaderText.text);
        if (oneLifeContinueButtonText != null) oneLifeContinueButtonText.text = LocalizationManager.GetText("OneLife.ContinueButton", oneLifeContinueButtonText.text);

        if (closeButtonText != null) closeButtonText.text = LocalizationManager.GetText("Characters.CloseButton", closeButtonText.text);
        if (narrationButtonText != null) narrationButtonText.text = LocalizationManager.GetText("Characters.NarrationButton", narrationButtonText.text);
        if (backButtonText != null) backButtonText.text = LocalizationManager.GetText("Characters.BackButton", backButtonText.text);

        if (creditsTitleText != null) creditsTitleText.text = LocalizationManager.GetText("Credits.Title", creditsTitleText.text);
        if (programmingTitleText != null) programmingTitleText.text = LocalizationManager.GetText("Credits.ProgrammingTitle", programmingTitleText.text);
        if (artLabelText != null) artLabelText.text = LocalizationManager.GetText("Credits.ArtLabel", artLabelText.text);
        if (portraitsLabelText != null) portraitsLabelText.text = LocalizationManager.GetText("Credits.PortraitsLabel", portraitsLabelText.text);
        if (uiDesignLabelText != null) uiDesignLabelText.text = LocalizationManager.GetText("Credits.UIDesignLabel", uiDesignLabelText.text);
        if (writingLabelText != null) writingLabelText.text = LocalizationManager.GetText("Credits.WritingLabel", writingLabelText.text);
        if (audioLabelText != null) audioLabelText.text = LocalizationManager.GetText("Credits.AudioLabel", audioLabelText.text);
        if (charactersExitButtonText != null) charactersExitButtonText.text = LocalizationManager.GetText("Credits.BackButton", charactersExitButtonText.text);
        if (creditsNameBajnokText != null) creditsNameBajnokText.text = LocalizationManager.GetText("Credits.Name.Bajnok", creditsNameBajnokText.text);
        if (creditsNameGaluszText != null) creditsNameGaluszText.text = LocalizationManager.GetText("Credits.Name.Galusz", creditsNameGaluszText.text);
        if (creditsNameBartaText != null) creditsNameBartaText.text = LocalizationManager.GetText("Credits.Name.Barta", creditsNameBartaText.text);
        if (creditsNameNagyBorusText != null) creditsNameNagyBorusText.text = LocalizationManager.GetText("Credits.Name.NagyBorus", creditsNameNagyBorusText.text);
        if (creditsNameBendaText != null) creditsNameBendaText.text = LocalizationManager.GetText("Credits.Name.Benda", creditsNameBendaText.text);
        if (creditsNameBorosText != null) creditsNameBorosText.text = LocalizationManager.GetText("Credits.Name.Boros", creditsNameBorosText.text);
        if (hungarianNarrationLabelText != null) hungarianNarrationLabelText.text = LocalizationManager.GetText("Credits.HungarianNarrationLabel", hungarianNarrationLabelText.text);
        if (creditsNameNagySandorText != null) creditsNameNagySandorText.text = LocalizationManager.GetText("Credits.Name.NagySandor", creditsNameNagySandorText.text);
        if (englishNarrationLabelText != null) englishNarrationLabelText.text = LocalizationManager.GetText("Credits.EnglishNarrationLabel", englishNarrationLabelText.text);
        if (creditsNameHulseText != null) creditsNameHulseText.text = LocalizationManager.GetText("Credits.Name.Hulse", creditsNameHulseText.text);

        settingsTitleText.text = LocalizationManager.GetText("Settings.Title", settingsTitleText.text);
        languageLabelText.text = LocalizationManager.GetText("Settings.LanguageLabel", languageLabelText.text);
        languageButtonText.text = LocalizationManager.GetText("Settings.LanguageButton.EnglishHungarian", languageButtonText.text);
        subtitlesLabelText.text = LocalizationManager.GetText("Settings.SubtitlesLabel", subtitlesLabelText.text);

        if (subtitlesEnabled)
        {
            subtitlesButtonText.text = LocalizationManager.GetText("Settings.SubtitlesOn", subtitlesButtonText.text);
        }
        else
        {
            subtitlesButtonText.text = LocalizationManager.GetText("Settings.SubtitlesOff", subtitlesButtonText.text);
        }

        subtitlesLanguageLabelText.text = LocalizationManager.GetText("Settings.SubtitlesLanguageLabel", subtitlesLanguageLabelText.text);

        if (subtitlesLanguage == "English")
        {
            subtitlesLanguageButtonText.text = LocalizationManager.GetText("Settings.SubtitlesLanguageButton.EnglishHungarian", subtitlesLanguageButtonText.text);
        }
        else
        {
            subtitlesLanguageButtonText.text = LocalizationManager.GetText("Settings.SubtitlesLanguageButton.HungarianEnglish", subtitlesLanguageButtonText.text);
        }

        settingsBackButtonText.text = LocalizationManager.GetText("Settings.BackButton", settingsBackButtonText.text);
    }

    public void SetLanguage()
    {
        if (language == "English")
        {
            language = "Hungarian";
        }
        else
        {
            language = "English";
        }

        LocalizationManager.SetLanguage(language);
        language = LocalizationManager.CurrentLanguage;
        ChangeLanguage();
        SaveSoldiersToFile();
    }

    public void ToggleSubtitles()
    {
        subtitlesEnabled = !subtitlesEnabled;
        PlayerPrefs.SetInt("SubtitlesEnabled", subtitlesEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ChangeLanguage();
        HideSubtitleLanguageOption();
        SaveSoldiersToFile();
    }

    public void HideSubtitleLanguageOption()
    {
        if(subtitlesEnabled)
        {
            subtitlesLanguageLabel.gameObject.SetActive(true);
            subtitlesLanguageButton.gameObject.SetActive(true);
        }
        else
        {
            subtitlesLanguageLabel.gameObject.SetActive(false);
            subtitlesLanguageButton.gameObject.SetActive(false);
        }
    }

    public void ToggleSubtitlesLanguage()
    {
        if (subtitlesLanguage == "English")
        {
            subtitlesLanguage = "Hungarian";
        }
        else
        {
            subtitlesLanguage = "English";
        }
        PlayerPrefs.SetString("SubtitlesLanguage", subtitlesLanguage);
        PlayerPrefs.Save();
        ChangeLanguage();
        SaveSoldiersToFile();
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SaveSoldiersToFile() 
    {
        List<SoldierSaveData> saveData = new List<SoldierSaveData>();

        if (HungarianSoldiers != null)
            foreach (var soldier in HungarianSoldiers) 
                saveData.Add(new SoldierSaveData { Name = soldier.Name, isOpened = soldier.isOpened });
        
        if (RussianSoldiers != null)
            foreach (var soldier in RussianSoldiers) 
                saveData.Add(new SoldierSaveData { Name = soldier.Name, isOpened = soldier.isOpened });

        SaveWrapper wrapper = new SaveWrapper 
        {
            Soldiers = saveData,
            NarrationLanguage = language, // Nyelv mentése
            SubtitlesEnabled = subtitlesEnabled,          // Felirat állapot mentése
            SubtitlesLanguage = subtitlesLanguage            // Felirat nyelv mentése
        };
        Debug.Log("✅ Save file created with Narration Language: " + wrapper.NarrationLanguage + ", Subtitles Enabled: " + wrapper.SubtitlesEnabled + ", Subtitles Language: " + wrapper.SubtitlesLanguage);

        string json = JsonUtility.ToJson(wrapper, true);
        string filePath = Path.Combine(Application.persistentDataPath, "SoldiersData.json");
        File.WriteAllText(filePath, json);
    }

    public void LoadSoldiersFromFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "SoldiersData.json");
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("❌ Save file not found: " + filePath);
            language = PlayerPrefs.GetString(LocalizationManager.MenuLanguagePrefKey, "English");
            subtitlesEnabled = PlayerPrefs.GetInt("SubtitlesEnabled", 1) == 1;
            subtitlesLanguage = PlayerPrefs.GetString("SubtitlesLanguage", "English");
            UpdateCharacterButtonsVisuals();
            return;
        }

        string json = File.ReadAllText(filePath);
        SaveWrapper saveData = JsonUtility.FromJson<SaveWrapper>(json);

        Debug.Log("✅ Save file loaded successfully: " + saveData.NarrationLanguage + ", Subtitles Enabled: " + saveData.SubtitlesEnabled + ", Subtitles Language: " + saveData.SubtitlesLanguage);

        foreach (var soldierData in saveData.Soldiers)
        {
            SoldierData soldier = System.Array.Find(HungarianSoldiers, s => s.Name == soldierData.Name) ??
                                System.Array.Find(RussianSoldiers, s => s.Name == soldierData.Name);

            if (soldier != null)
            {
                soldier.isOpened = soldierData.isOpened;
            }
        }

        // Betöltjük a nyelvet és a felirat állapotát
        language = PlayerPrefs.GetString(LocalizationManager.MenuLanguagePrefKey, saveData.NarrationLanguage);
        if (string.IsNullOrEmpty(language))
        {
            language = "English";
        }

        subtitlesEnabled = PlayerPrefs.GetInt("SubtitlesEnabled", saveData.SubtitlesEnabled ? 1 : 0) == 1;
        subtitlesLanguage = PlayerPrefs.GetString("SubtitlesLanguage", saveData.SubtitlesLanguage);
        if (string.IsNullOrEmpty(subtitlesLanguage))
        {
            subtitlesLanguage = "English";
        }

        UpdateCharacterButtonsVisuals();
    }

    private void UpdateCharacterButtonsVisuals()
    {
        // A gomb-objektumok nevei NEM a rajtuk levo portret koveti (a "Button_Andras" peldaul
        // Folhoffer Benedek kepet mutatja), ezert a parositas a gombon latszo portre es a
        // CharacterSelector indexe alapjan keszult, nem a GameObject neve alapjan.
        ApplyCharacterButtonVisual(button1, IsCharacterUnlocked(HungarianSoldiers, 2)); // Folhoffer Benedek
        ApplyCharacterButtonVisual(button2, IsCharacterUnlocked(HungarianSoldiers, 1)); // Ferenc Bedo
        ApplyCharacterButtonVisual(button3, IsCharacterUnlocked(HungarianSoldiers, 3)); // Istvan Maruzs
        ApplyCharacterButtonVisual(button4, IsCharacterUnlocked(HungarianSoldiers, 0)); // Andras Hodanics
        ApplyCharacterButtonVisual(button5, IsCharacterUnlocked(RussianSoldiers, 3));   // Serik Nurgaliyev
        ApplyCharacterButtonVisual(button6, IsCharacterUnlocked(RussianSoldiers, 1));   // Ivan Petrov
        ApplyCharacterButtonVisual(button7, IsCharacterUnlocked(RussianSoldiers, 0));   // Aleksei Morozov
        ApplyCharacterButtonVisual(button8, IsCharacterUnlocked(RussianSoldiers, 2));   // Mykola Shevchenko
    }

    // IDEIGLENES: az unlockAllCharacters bekapcsolva minden karaktert kattinthatova tesz,
    // hogy teszteles kozben ne kelljen vegighallgatni a narraciokat. Kikapcsolva a
    // mentesbol betoltott isOpened dont, pontosan ugy, ahogy eddig.
    private bool IsCharacterUnlocked(SoldierData[] soldiers, int index)
    {
        if (unlockAllCharacters)
        {
            return true;
        }

        if (soldiers == null || index < 0 || index >= soldiers.Length || soldiers[index] == null)
        {
            return false;
        }

        return soldiers[index].isOpened;
    }

    private void ApplyCharacterButtonVisual(Button button, bool isOpened)
    {
        if (button == null) return;

        button.interactable = isOpened;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        Color disabledColor = colors.disabledColor;
        disabledColor.a = lockedButtonAlpha;
        colors.disabledColor = disabledColor;
        button.colors = colors;

        // Button.colors only takes effect with the "Color Tint" transition; most of our
        // buttons use Sprite Swap/Animation, so the graphic's alpha is set directly too.
        if (button.targetGraphic != null)
        {
            Color graphicColor = button.targetGraphic.color;
            graphicColor.a = isOpened ? 1f : lockedButtonAlpha;
            button.targetGraphic.color = graphicColor;
        }

        Shadow shadow = button.GetComponent<Shadow>();
        if (isOpened)
        {
            shadow ??= button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = buttonShadowColor;
            shadow.effectDistance = buttonShadowDistance;
            shadow.enabled = true;
        }
        else if (shadow is not null)
        {
            shadow.enabled = false;
        }
    }
}
