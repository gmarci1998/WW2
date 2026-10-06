using UnityEngine;
using TMPro;

/// <summary>
/// Feliratsorok animált megjelenítése TextMeshPro szövegen.
/// Beúszás: a szavak egyesével tűnnek elő, és közben egy kicsit lentebbről emelkednek a helyükre.
/// Kiúszás: az egész sor egyszerre halványodik el, függőleges mozgás nélkül.
/// A TMP csúcspontjait módosítja közvetlenül, ezért nem kell hozzá külön shader vagy anyag.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class SubtitleTextAnimator : MonoBehaviour
{
    [Header("Beúszás (szavanként)")]
    [Tooltip("Ennyi másodperc telik el két egymást követő szó indulása között.")]
    [Min(0f)] [SerializeField] private float wordStagger = 0.06f;

    [Tooltip("Egy szó ennyi idő alatt ér el a teljes láthatóságig.")]
    [Min(0.01f)] [SerializeField] private float wordFadeDuration = 0.28f;

    [Tooltip("Ennyivel indul lentebbről egy szó (a szöveg saját egységében, kb. pixel).")]
    [SerializeField] private float riseDistance = 10f;

    [Header("Kiúszás")]
    [Tooltip("Az egész sor ennyi idő alatt halványodik el.")]
    [Min(0.01f)] [SerializeField] private float fadeOutDuration = 0.2f;

    private enum Phase { Hidden, FadingIn, Visible, FadingOut }

    private TMP_Text text;
    private TMP_MeshInfo[] cachedMesh;   // az érintetlen csúcspontok, ehhez képest tolunk el
    private int[] wordOfChar;            // karakterindex -> szóindex
    private int wordCount;

    private Phase phase = Phase.Hidden;
    private float phaseTime;
    private float frozenFadeInTime;      // ha beúszás közben szakítjuk meg, innen folytatjuk a kiúszást

    private string pendingText;
    private bool hasPending;

    public float FadeOutDuration { get { return fadeOutDuration; } }
    public bool IsHidden { get { return phase == Phase.Hidden; } }
    public bool IsFadingOut { get { return phase == Phase.FadingOut; } }

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        text.text = "";
    }

    void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    /// <summary>
    /// Új sor megjelenítése. Ha még látszik valami, előbb azt kiúsztatja, és utána indul az új.
    /// </summary>
    public void ShowText(string newText)
    {
        if (string.IsNullOrEmpty(newText))
        {
            Hide();
            return;
        }

        if (phase == Phase.Hidden)
        {
            StartFadeIn(newText);
            return;
        }

        pendingText = newText;
        hasPending = true;
        BeginFadeOut();
    }

    /// <summary>
    /// Kiúszás indítása. A Subtitles hívja a sor vége előtt, hogy a következő sor pont időben jöhessen.
    /// </summary>
    public void BeginFadeOut()
    {
        if (phase == Phase.Hidden || phase == Phase.FadingOut)
        {
            return;
        }

        frozenFadeInTime = (phase == Phase.FadingIn) ? phaseTime : float.MaxValue;
        phase = Phase.FadingOut;
        phaseTime = 0f;
    }

    /// <summary>Kiúsztatás, utána üres marad.</summary>
    public void Hide()
    {
        hasPending = false;
        pendingText = null;
        BeginFadeOut();
    }

    /// <summary>Azonnali ürítés, animáció nélkül (pl. új narráció indulásakor).</summary>
    public void HideImmediate()
    {
        hasPending = false;
        pendingText = null;
        phase = Phase.Hidden;
        phaseTime = 0f;
        text.text = "";
    }

    void Update()
    {
        if (phase == Phase.Hidden || phase == Phase.Visible)
        {
            return;
        }

        phaseTime += Time.deltaTime;

        if (phase == Phase.FadingOut)
        {
            float fade = 1f - Mathf.Clamp01(phaseTime / Mathf.Max(fadeOutDuration, 0.0001f));
            ApplyVertices(frozenFadeInTime, fade);

            if (phaseTime >= fadeOutDuration)
            {
                text.text = "";
                phase = Phase.Hidden;

                if (hasPending)
                {
                    hasPending = false;
                    string next = pendingText;
                    pendingText = null;
                    StartFadeIn(next);
                }
            }
            return;
        }

        // FadingIn
        float total = Mathf.Max(wordCount - 1, 0) * wordStagger + wordFadeDuration;
        ApplyVertices(phaseTime, 1f);

        if (phaseTime >= total)
        {
            ApplyVertices(float.MaxValue, 1f);
            phase = Phase.Visible;
        }
    }

    private void StartFadeIn(string s)
    {
        text.text = s;
        text.ForceMeshUpdate();
        CacheMesh();

        phase = Phase.FadingIn;
        phaseTime = 0f;
        ApplyVertices(0f, 1f);
    }

    /// <summary>
    /// Elmenti az eredeti csúcspontokat, és minden karakterhez kiszámolja, hányadik szóhoz tartozik.
    /// A szóhatár a szóköz; az írásjelek az előttük álló szóval együtt mozognak.
    /// </summary>
    private void CacheMesh()
    {
        TMP_TextInfo info = text.textInfo;
        cachedMesh = info.CopyMeshInfoVertexData();

        int n = info.characterCount;
        if (wordOfChar == null || wordOfChar.Length < n)
        {
            wordOfChar = new int[Mathf.Max(n, 16)];
        }

        int w = -1;
        bool inWord = false;
        for (int i = 0; i < n; i++)
        {
            bool whitespace = char.IsWhiteSpace(info.characterInfo[i].character);
            if (!whitespace && !inWord)
            {
                w++;
                inWord = true;
            }
            else if (whitespace)
            {
                inWord = false;
            }
            wordOfChar[i] = Mathf.Max(w, 0);
        }
        wordCount = Mathf.Max(w + 1, 1);
    }

    /// <summary>
    /// A csúcspontok beállítása egy adott beúszási időponthoz és egy globális halványítási szorzóhoz.
    /// fadeInTime = float.MaxValue: minden szó a végleges helyén, teljes alfával.
    /// </summary>
    private void ApplyVertices(float fadeInTime, float globalAlpha)
    {
        if (cachedMesh == null)
        {
            return;
        }

        TMP_TextInfo info = text.textInfo;
        int n = info.characterCount;

        for (int i = 0; i < n; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible)
            {
                continue;
            }

            int m = c.materialReferenceIndex;
            if (m >= cachedMesh.Length || m >= info.meshInfo.Length)
            {
                continue;
            }

            float p = (fadeInTime == float.MaxValue)
                ? 1f
                : Mathf.Clamp01((fadeInTime - wordOfChar[i] * wordStagger) / Mathf.Max(wordFadeDuration, 0.0001f));

            float eased = 1f - (1f - p) * (1f - p) * (1f - p); // ease-out cubic: gyorsan indul, puhán érkezik
            float yOffset = -riseDistance * (1f - eased);
            float alpha = p * globalAlpha;

            Vector3[] src = cachedMesh[m].vertices;
            Vector3[] dst = info.meshInfo[m].vertices;
            Color32[] srcCol = cachedMesh[m].colors32;
            Color32[] dstCol = info.meshInfo[m].colors32;

            int v = c.vertexIndex;
            if (v + 3 >= dst.Length || v + 3 >= src.Length)
            {
                continue;
            }

            for (int k = 0; k < 4; k++)
            {
                dst[v + k] = src[v + k] + new Vector3(0f, yOffset, 0f);

                Color32 col = srcCol[v + k];
                col.a = (byte)Mathf.RoundToInt(col.a * alpha);
                dstCol[v + k] = col;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>
    /// Ha a TMP valamiért újragenerálja a hálót animáció közben (pl. felbontásváltás),
    /// az eredeti csúcspontokat újra el kell menteni, különben elcsúszna az eltolás.
    /// </summary>
    private void OnTextChanged(Object changed)
    {
        if (changed != text || phase == Phase.Hidden)
        {
            return;
        }

        if (text.textInfo.characterCount == 0)
        {
            return;
        }

        CacheMesh();
    }
}
