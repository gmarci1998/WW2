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
        StartCoroutine(CheckCreditsOnAwake());
    }

    IEnumerator CheckCreditsOnAwake()
    {
        yield return null; 
        
        if (GameManager.Instance != null && mainMenu != null && creditsMenu != null)
        {
            bool shouldShowCredits = GameManager.Instance.showCreditsOnGameEnd;
            GameManager.Instance.showCreditsOnGameEnd = false;
            
            if (shouldShowCredits)
            {
                // Főmenü elrejtése
                mainMenu.alpha = 0f;
                mainMenu.gameObject.SetActive(false);
                
                StartCoroutine(OpenCreditsSafe());
                yield break;
            }
        }
    }

    IEnumerator OpenCreditsSafe()
    {
        yield return new WaitForEndOfFrame(); // Biztos stabilitás
        
        // Credits aktiválása
        creditsMenu.gameObject.SetActive(true);
        creditsMenu.alpha = 0f;
        
        // Gyors fade-in credits-re
        StartCoroutine(Fade(creditsMenu, 0f, 1f));
        creditsMenu.interactable = true;
        creditsMenu.blocksRaycasts = true;
        
        // Inicializálás
        LoadSoldiersFromFile();
        UnlockCursor();
    }

    void Start()  
    {
        LoadSoldiersFromFile();
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
        settingsTitleText.text = language == "English" ? "Settings" : "Beállítások";
        languageLabelText.text = language == "English" ? "Language" : "Nyelv";
        languageButtonText.text = language == "English" ? "   <b>English</b> / Hungarian" : "Angol / <b>Magyar</b>";
        subtitlesLabelText.text = language == "English" ? "Subtitles" : "Feliratok";
        if (subtitlesEnabled)
        {
                subtitlesButtonText.text = language == "English" ? "<b>ON</b> / OFF" : "<b>BE</b> / KI";
            }
            else
            {
                subtitlesButtonText.text = language == "English" ? "ON / <b>OFF</b>" : "BE / <b>KI</b>";
        }
        subtitlesLanguageLabelText.text = language == "English" ? "Subtitles Language" : "Felirat nyelve";
        if (subtitlesLanguage == "English")
        {
                subtitlesLanguageButtonText.text = language == "English" ? "    <b>English</b> / Hungarian" : "<b>Angol</b> / Magyar";
            }
            else
            {
                subtitlesLanguageButtonText.text = language == "English" ? "   English / <b>Hungarian</b>" : "Angol / <b>Magyar</b>";
        }
        settingsBackButtonText.text = language == "English" ? "Back" : "Vissza";
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
        PlayerPrefs.SetString("NarrationLanguage", language);
        PlayerPrefs.Save();
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
            language = PlayerPrefs.GetString("NarrationLanguage", "English");
            subtitlesEnabled = PlayerPrefs.GetInt("SubtitlesEnabled", 1) == 1;
            subtitlesLanguage = PlayerPrefs.GetString("SubtitlesLanguage", "English");
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
        language = PlayerPrefs.GetString("NarrationLanguage", saveData.NarrationLanguage);
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

        if (!HungarianSoldiers[0].isOpened) button1.interactable = false;
        if (!HungarianSoldiers[1].isOpened) button2.interactable = false;
        if (!HungarianSoldiers[2].isOpened) button3.interactable = false;
        if (!HungarianSoldiers[3].isOpened) button4.interactable = false;
        if (!RussianSoldiers[0].isOpened) button5.interactable = false;
        if (!RussianSoldiers[1].isOpened) button6.interactable = false;
        if (!RussianSoldiers[2].isOpened) button7.interactable = false;
        if (!RussianSoldiers[3].isOpened) button8.interactable = false;
    }
}
