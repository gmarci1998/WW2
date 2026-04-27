using TMPro;
using UnityEngine;

public class ControlsHint : MonoBehaviour
{
    private const string LanguagePrefsKey = "NarrationLanguage";

    public enum Language
    {
        English,
        Hungarian
    }

    [SerializeField] private TMP_Text controlsText;
    [SerializeField] private Language fallbackLanguage = Language.English;

    private string appliedLanguage;

    private const string EnglishHint =
        "Press SPACE to crouch or stand up.\n" +
        "Move the weapon with the mouse.\n" +
        "Shoot with the LEFT MOUSE BUTTON.";

    private const string HungarianHint =
        "A guggoláshoz és a felálláshoz nyomd meg a SPACE billentyűt.\n" +
        "Az egérrel mozgathatod a fegyvert.\n" +
        "Lőni a BAL EGÉRGOMBBAL tudsz.";

    void Awake()
    {
        if (controlsText == null)
        {
            controlsText = GetComponent<TMP_Text>();
        }
    }

    void Start()
    {
        ApplyText();
    }

    void Update()
    {
        string savedLanguage = GetSavedLanguage();
        if (savedLanguage != appliedLanguage)
        {
            ApplyText(savedLanguage);
        }
    }

    void OnValidate()
    {
        if (controlsText == null)
        {
            controlsText = GetComponent<TMP_Text>();
        }

        ApplyText(fallbackLanguage.ToString());
    }

    void ApplyText()
    {
        ApplyText(GetSavedLanguage());
    }

    void ApplyText(string selectedLanguage)
    {
        if (controlsText == null)
        {
            return;
        }

        appliedLanguage = NormalizeLanguage(selectedLanguage);
        controlsText.text = GetHintText(appliedLanguage);
    }

    string GetSavedLanguage()
    {
        string savedLanguage = PlayerPrefs.GetString(LanguagePrefsKey, fallbackLanguage.ToString());
        return NormalizeLanguage(savedLanguage);
    }

    string NormalizeLanguage(string selectedLanguage)
    {
        if (selectedLanguage == Language.Hungarian.ToString())
        {
            return Language.Hungarian.ToString();
        }

        return Language.English.ToString();
    }

    string GetHintText(string selectedLanguage)
    {
        if (selectedLanguage == Language.Hungarian.ToString())
        {
            return HungarianHint;
        }

        return EnglishHint;
    }
}
