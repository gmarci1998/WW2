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

    [SerializeField] private TextMeshProUGUI menuTitleText;

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
        UnlockCursor();
    }

    
    IEnumerator OpenCreditsImmediate()
    {
        yield return null; 
        if (creditsMenu != null)
        {
            creditsMenu.gameObject.SetActive(true);
            StartCoroutine(SwitchMenu(null, creditsMenu));
        }
        LoadSoldiersFromFile();

        UnlockCursor();
    }

    public void ChangeLanguage()
    {
        menuTitleText.text = language == "English" ? "Play game" : "Játék indítása";
    }

    public void SetLanguage()
    {
        if (language == "English")
        {
            language = "Hungarian";
        }
        else
        {
            PlayerPrefs.SetString("NarrationLanguage", "Hungarian");
            PlayerPrefs.SetString("SubtitlesLanguage", "Hungarian");
        }
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadSoldiersFromFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "SoldiersData.json");
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("❌ Save file not found: " + filePath);
            return;
        }

        string json = File.ReadAllText(filePath);
        SaveWrapper saveData = JsonUtility.FromJson<SaveWrapper>(json);

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
        PlayerPrefs.SetString("NarrationLanguage", saveData.NarrationLanguage);
        PlayerPrefs.SetInt("SubtitlesEnabled", saveData.SubtitlesEnabled ? 1 : 0);
        PlayerPrefs.SetString("SubtitlesLanguage", saveData.SubtitlesLanguage);

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
