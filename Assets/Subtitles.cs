using UnityEngine;
using TMPro;

public class Subtitles : MonoBehaviour
{
    public TextMeshProUGUI subtitleText;
    public SubtitleEntry[] subtitleEntries; // Feliratsorok kezdéssel és végponttal

    [Tooltip("A narráció hangforrása. Ehhez igazodik a felirat, hogy ne csússzon szét.")]
    [SerializeField] private AudioSource narrationSource;

    [Tooltip("A sorok be- és kiúsztatását végző komponens. Ha üres, a szöveg objektumáról vesszük (vagy oda tesszük).")]
    [SerializeField] private SubtitleTextAnimator animator;

    [Tooltip("Milyen sűrűn olvassa újra a felirat be/ki beállítást (másodperc).")]
    [SerializeField] private float settingsPollInterval = 0.5f;

    private int currentIndex;
    private int shownIndex = -1;    // melyik sor van kint (vagy éppen úszik ki); -1 = semmi
    private float elapsedTime;      // tartalék óra, ha nincs narrációs hangforrás
    private float audioTime;        // az utolsó ismert lejátszási pozíció
    private bool narrationStarted;
    private bool narrationFinished;
    private bool subtitlesEnabled = true;
    private float settingsPollTimer;

    void Awake()
    {
        // Már Awake-ben, mert a GameManager a Start előtt is hívhatja a SetSubtitles-t.
        EnsureAnimator();
    }

    void Start()
    {
        RefreshSettings();
        ClearImmediate();
    }

    /// <summary>
    /// A megjelenítendő feliratsorok beállítása. Mindig nulláról indítja a lejátszást.
    /// </summary>
    public void SetSubtitles(SubtitleEntry[] entries)
    {
        // Másolatot tartunk, hogy a végidők kitöltése ne írjon bele a katona adatába.
        subtitleEntries = (entries == null) ? new SubtitleEntry[0] : (SubtitleEntry[])entries.Clone();
        SubtitleLoader.FillMissingEnds(subtitleEntries);

        currentIndex = 0;
        elapsedTime = 0f;
        audioTime = 0f;
        narrationStarted = false;
        narrationFinished = false;

        RefreshSettings();
        ClearImmediate();
    }

    /// <summary>
    /// A narráció hangforrásának átadása. Enélkül a felirat saját órával fut (pontatlan).
    /// </summary>
    public void SetNarrationSource(AudioSource source)
    {
        narrationSource = source;
        audioTime = 0f;
        narrationStarted = false;
        narrationFinished = false;
    }

    /// <summary>
    /// Újraolvassa a felirat be/ki beállítást. A beállítások menü is hívhatja azonnali hatásért.
    /// </summary>
    public void RefreshSettings()
    {
        subtitlesEnabled = PlayerPrefs.GetInt("SubtitlesEnabled", 1) == 1;
        settingsPollTimer = 0f;

        if (!subtitlesEnabled)
        {
            ClearImmediate();
        }
    }

    void Update()
    {
        settingsPollTimer += Time.unscaledDeltaTime;
        if (settingsPollTimer >= settingsPollInterval)
        {
            bool wasEnabled = subtitlesEnabled;
            subtitlesEnabled = PlayerPrefs.GetInt("SubtitlesEnabled", 1) == 1;
            settingsPollTimer = 0f;

            if (wasEnabled && !subtitlesEnabled)
            {
                ClearImmediate();
            }
        }

        if (!subtitlesEnabled || subtitleEntries == null || subtitleEntries.Length == 0)
        {
            return;
        }

        float time;
        if (!TryGetPlaybackTime(out time))
        {
            Clear();
            return;
        }

        // Visszaugrás esetén (újraindított narráció) előlről keressük a sort.
        if (currentIndex > 0 && time < subtitleEntries[currentIndex - 1].time)
        {
            currentIndex = 0;
        }

        while (currentIndex < subtitleEntries.Length && time >= subtitleEntries[currentIndex].endTime)
        {
            currentIndex++;
        }

        if (currentIndex < subtitleEntries.Length && time >= subtitleEntries[currentIndex].time)
        {
            SubtitleEntry entry = subtitleEntries[currentIndex];
            Show(currentIndex, entry.text);

            // A sor vége előtt elindítjuk a kiúszást, hogy a következő sor pont a saját idejében jöhessen.
            if (animator != null && time >= entry.endTime - animator.FadeOutDuration)
            {
                animator.BeginFadeOut();
            }
        }
        else
        {
            // Szünet két sor között, vagy a narráció vége.
            Clear();
        }
    }

    /// <summary>
    /// A narráció aktuális pozíciója. False, ha még el sem indult, vagy már véget ért.
    /// Szünet alatt (fedezékben) az utolsó ismert pozíciót adja vissza, így a sor kint marad.
    /// </summary>
    private bool TryGetPlaybackTime(out float time)
    {
        if (narrationSource == null || narrationSource.clip == null)
        {
            // Tartalék: saját óra. Csak akkor pontos, ha a narráció azonnal indul.
            elapsedTime += Time.deltaTime;
            time = elapsedTime;
            return true;
        }

        if (narrationSource.isPlaying)
        {
            narrationStarted = true;
            narrationFinished = false;
            audioTime = narrationSource.time;
        }
        else if (narrationStarted && audioTime >= narrationSource.clip.length - 0.25f)
        {
            narrationFinished = true;
        }

        time = audioTime;
        return narrationStarted && !narrationFinished;
    }

    private void EnsureAnimator()
    {
        if (animator != null || subtitleText == null)
        {
            return;
        }

        animator = subtitleText.GetComponent<SubtitleTextAnimator>();
        if (animator == null)
        {
            animator = subtitleText.gameObject.AddComponent<SubtitleTextAnimator>();
            Debug.Log("Subtitles: SubtitleTextAnimator hozzáadva a(z) " + subtitleText.name + " objektumhoz. "
                + "Add hozzá kézzel is, ha az Inspectorban akarod hangolni az animációt.");
        }
    }

    private void Show(int index, string text)
    {
        if (subtitleText == null || shownIndex == index)
        {
            return;
        }

        shownIndex = index;

        if (animator != null)
        {
            animator.ShowText(text);
        }
        else
        {
            subtitleText.text = text;
        }
    }

    /// <summary>Kiúsztatja az aktuális sort (ha van).</summary>
    private void Clear()
    {
        if (subtitleText == null || shownIndex == -1)
        {
            return;
        }

        shownIndex = -1;

        if (animator != null)
        {
            animator.Hide();
        }
        else
        {
            subtitleText.text = "";
        }
    }

    /// <summary>Azonnal üríti a feliratot, animáció nélkül.</summary>
    private void ClearImmediate()
    {
        shownIndex = -1;

        if (animator != null)
        {
            animator.HideImmediate();
        }
        else if (subtitleText != null)
        {
            subtitleText.text = "";
        }
    }
}
