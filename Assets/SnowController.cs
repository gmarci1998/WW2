using System.Collections;
using UnityEngine;

// Idonkent szallingozo hoeses harom particle reteggel (tavoli, kozepso, kozeli), hogy terhatasa legyen.
// A havazas veletlen szunet utan indul, veletlen ideig tart (minDuration..maxDuration), es
// fokozatosan er fel / cseng le, hogy ne kapcsoljon be egyik pillanatrol a masikra.
// A retegek Local simulation space-ben futnak, igy a teljes reteget el lehet tolni az egerrel
// (parallax) - a mar lehullo pelyhek is vele mozognak. Az eltolast a GameManager adja at
// (SetParallax): minden reteg a hatter es a Trench elmozdulasa kozott mozog.
// A jelenetbe a Tools > WW2 > Create Snow menuponttal lehet letrehozni.
public class SnowController : MonoBehaviour
{
    [System.Serializable]
    public class SnowLayer
    {
        public ParticleSystem particles;
        [Tooltip("Az egeres mozgatasnal kit kovessen a reteg: 0 = pont ugy mozog, mint a hatter, 1 = pont ugy, mint a Trench, kozte aranyosan.")]
        [Range(0f, 1f)] public float followTrench;
        [Tooltip("Oldaliranyu sodrodas sebessege (egyseg/mp). Az iranyat a program a Fog mozgasahoz igazitja (magyar oldalon megfordul). Ha a Foggal ellentetesen sodrodik, adj meg negativ erteket.")]
        public float windSpeed;

        [System.NonSerialized] public float baseRate;
        [System.NonSerialized] public Vector3 basePosition;
    }

    [Tooltip("Apro, lassu, halvanyabb pelyhek a Fog mogott - ugy mozog, mint a hatter.")]
    [SerializeField] private SnowLayer farLayer = new SnowLayer { followTrench = 0f, windSpeed = 0.2f };
    [Tooltip("Kozepes meretu pelyhek a Fog elott - a hatter es a Trench mozgasa kozott.")]
    [SerializeField] private SnowLayer middleLayer = new SnowLayer { followTrench = 0.5f, windSpeed = 0.3f };
    [Tooltip("Kevesebb, nagy, gyors pelyhek a Trench es a fegyver elott (guggolva is latszik) - ugy mozog, mint a Trench.")]
    [SerializeField] private SnowLayer nearLayer = new SnowLayer { followTrench = 1f, windSpeed = 0.4f };

    [Header("Idozites (masodperc)")]
    [Tooltip("Egy havazas minimalis hossza (a fel- es lecsengessel egyutt).")]
    [Min(0f)] [SerializeField] private float minDuration = 10f;
    [Tooltip("Egy havazas maximalis hossza (a fel- es lecsengessel egyutt).")]
    [Min(0f)] [SerializeField] private float maxDuration = 60f;
    [Tooltip("Minimalis szunet ket havazas kozott.")]
    [Min(0f)] [SerializeField] private float minPause = 30f;
    [Tooltip("Maximalis szunet ket havazas kozott.")]
    [Min(0f)] [SerializeField] private float maxPause = 120f;
    [Min(0f)] [SerializeField] private float fadeInTime = 4f;
    [Min(0f)] [SerializeField] private float fadeOutTime = 5f;
    [Tooltip("Havazasonkent veletlen erosseg - szorzo a reszecskerendszerekben beallitott Rate over Time ertekre.")]
    [SerializeField] private Vector2 intensityRange = new Vector2(0.6f, 1f);
    [Tooltip("Ha be van kapcsolva, az elso havazas rogton a jelenet indulasakor kezdodik (teszteleshez hasznos).")]
    [SerializeField] private bool snowOnStart = false;

    private SnowLayer[] layers;
    private float windDirection = 1f;

    void Awake()
    {
        layers = new[] { farLayer, middleLayer, nearLayer };

        foreach (SnowLayer layer in layers)
        {
            if (layer.particles == null) continue;

            // A particle rendszerben beallitott rate a teljes erosseg; jatek kozben ezt
            // skalazzuk 0 es 1 kozott.
            ParticleSystem.EmissionModule emission = layer.particles.emission;
            layer.baseRate = emission.rateOverTimeMultiplier;
            emission.rateOverTimeMultiplier = 0f;
            layer.basePosition = layer.particles.transform.localPosition;
        }

        ApplyWind();
    }

    void OnEnable()
    {
        StartCoroutine(SnowCycle(snowOnStart));
    }

    // A GameManager hivja minden frame-ben a hatter es a Trench aktualis egeres eltolasaval.
    public void SetParallax(Vector2 backgroundOffset, Vector2 trenchOffset)
    {
        if (layers == null) return;

        foreach (SnowLayer layer in layers)
        {
            if (layer.particles == null) continue;

            Vector2 offset = Vector2.Lerp(backgroundOffset, trenchOffset, layer.followTrench);
            layer.particles.transform.localPosition = layer.basePosition + (Vector3)offset;
        }
    }

    // A GameManager hivja, amikor a Fog iranyat beallitja (fogDirection: 1 orosz, -1 magyar oldal).
    // A Fog shader Time * Speed-del tolja az UV-t (Speed.x > 0), igy fogDirection = 1 eseten a
    // Fog balra uszik - a ho ugyanarra sodrodik.
    public void SetWindDirection(float fogDirection)
    {
        windDirection = fogDirection < 0f ? 1f : -1f;
        ApplyWind();
    }

    [ContextMenu("Havazas inditasa most")]
    private void StartSnowNow()
    {
        if (!Application.isPlaying) return;

        StopAllCoroutines();
        StartCoroutine(SnowCycle(true));
    }

    IEnumerator SnowCycle(bool startImmediately)
    {
        if (!startImmediately)
        {
            yield return new WaitForSeconds(Random.Range(minPause, Mathf.Max(minPause, maxPause)));
        }

        while (true)
        {
            yield return Snowfall(Random.Range(minDuration, Mathf.Max(minDuration, maxDuration)));
            yield return new WaitForSeconds(Random.Range(minPause, Mathf.Max(minPause, maxPause)));
        }
    }

    IEnumerator Snowfall(float duration)
    {
        float intensity = Random.Range(intensityRange.x, intensityRange.y);

        // Rovid havazasnal a fel- es lecsengest aranyosan roviditjuk, hogy beleferjen.
        float fadeScale = Mathf.Min(1f, duration / Mathf.Max(0.01f, fadeInTime + fadeOutTime));
        float fadeIn = fadeInTime * fadeScale;
        float fadeOut = fadeOutTime * fadeScale;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float rampIn = fadeIn > 0f ? t / fadeIn : 1f;
            float rampOut = fadeOut > 0f ? (duration - t) / fadeOut : 1f;
            SetEmission(intensity * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(rampIn, rampOut))));
            yield return null;
        }

        // A kibocsatas leall, a mar levegoben levo pelyhek termeszetesen lehullanak.
        SetEmission(0f);
    }

    void SetEmission(float amount)
    {
        foreach (SnowLayer layer in layers)
        {
            if (layer.particles == null) continue;

            ParticleSystem.EmissionModule emission = layer.particles.emission;
            emission.rateOverTimeMultiplier = layer.baseRate * amount;
        }
    }

    void ApplyWind()
    {
        if (layers == null) return;

        foreach (SnowLayer layer in layers)
        {
            if (layer.particles == null) continue;

            // A Velocity over Lifetime tengelyeinek azonos modban kell lenniuk (Two Constants),
            // ezert az X is ket konstans - pelyhenkent kicsit eltero sodrodas.
            ParticleSystem.VelocityOverLifetimeModule velocity = layer.particles.velocityOverLifetime;
            float wind = layer.windSpeed * windDirection;
            velocity.x = new ParticleSystem.MinMaxCurve(wind * 0.6f, wind * 1.4f);
        }
    }
}
