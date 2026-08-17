using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OneLifeSequence : MonoBehaviour, IPointerClickHandler
{
    private struct LifeItem
    {
        public string numberKey;
        public string numberFallback;
        public string descriptionKey;
        public string descriptionFallback;
    }

    private struct CompareItem
    {
        public string numberKey;
        public string numberFallback;
        public string captionKey;
        public string captionFallback;
        public float value;
        public Color tint;
    }

    private class RowRefs
    {
        public GameObject root;
        public TextMeshProUGUI numberText;
        public TextMeshProUGUI descriptionText;
        public int itemIndex;
    }

    [Header("Timing")]
    [SerializeField] private float stepDuration = 15f;
    [SerializeField] private float phase2StepDuration = 25f;
    [SerializeField] private float fadeTime = 0.4f;
    [SerializeField] private float barScrollSpeed = 350f;
    [SerializeField] private float barScrollRampTime = 1f;

    [Header("Shared Assets")]
    [SerializeField] private Sprite onePersonSprite;
    [SerializeField] private Sprite manyPeopleSprite;
    [SerializeField] private TMP_FontAsset numberFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    [Header("Layout")]
    [SerializeField] private float bigSquareIntroSize = 90f;
    [SerializeField] private float bigSquareGrownSize = 350f;
    [SerializeField] private float barHeight = 320f;
    [SerializeField] private float barGap = 24f;
    [SerializeField] private float rowSpacing = 18f;

    [Header("Phase 1 refs")]
    [SerializeField] private RectTransform phase1Root;
    [SerializeField] private TextMeshProUGUI introTitleText;
    [SerializeField] private RectTransform bigSquare;
    [SerializeField] private Image bigSquareImage;
    [SerializeField] private RectTransform[] treemapRectSlots;
    [SerializeField] private RectTransform listContainer;
    [SerializeField] private TextMeshProUGUI headerText;

    [Header("Phase 2 refs")]
    [SerializeField] private RectTransform phase2Root;
    [SerializeField] private RectTransform barsViewport;
    [SerializeField] private RectTransform barsTrack;
    [SerializeField] private TextMeshProUGUI currentBarNumberText;
    [SerializeField] private TextMeshProUGUI currentBarCaptionText;
    [SerializeField] private float compareTextFadeTime = 0.35f;
    [SerializeField, Range(0f, 1f)] private float compareTextSwapProgress = 0.8f;

    [Header("Continue")]
    [SerializeField] private CanvasGroup continueButtonGroup;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private RectTransform transitionSquare;
    [SerializeField] private Image transitionSquareImage;
    [SerializeField] private Image transitionBlackout;
    [SerializeField] private float continueGrowDuration = 5f;
    [SerializeField] private float blackoutHoldDuration = 2f;

    private static readonly Color PastItemColor = new Color(0.85f, 0.85f, 0.85f, 0.30f);
    private static readonly Color CurrentItemColor = new Color(0.82f, 0.11f, 0.08f, 0.80f);
    private static readonly Color IntroSquareColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    public static readonly Rect[] TreemapRects =
    {
        new Rect(0f, 0.85f, 0.09f, 0.15f),      // Row1 - 80 birthdays
        new Rect(0.09f, 0.85f, 0.127f, 0.15f),  // Row2 - 960 months
        new Rect(0f, 0.64f, 0.108f, 0.21f),     // Row3 - 1,000 full moons
        new Rect(0.108f, 0.64f, 0.109f, 0.21f), // Row4 - 4,000 weekends
        new Rect(0.217f, 0.64f, 0.186f, 0.36f), // Row5 - 7,000 songs
        new Rect(0.403f, 0.64f, 0.217f, 0.36f), // Row6 - 10,000 people met
        new Rect(0f, 0.50f, 0.62f, 0.14f),      // Row7 - 15,000 conversations
        new Rect(0f, 0f, 0.62f, 0.50f),         // Row8 - 20,000 walks
        new Rect(0.62f, 0f, 0.38f, 1.0f),       // Row9 - 29,000 dinners
    };

    private static readonly LifeItem[] LifeItems =
    {
        new LifeItem { numberKey = "OneLife.Contains.Row1.Number", numberFallback = "80",
            descriptionKey = "OneLife.Contains.Row1.Description", descriptionFallback = "About 80 birthdays." },
        new LifeItem { numberKey = "OneLife.Contains.Row2.Number", numberFallback = "960",
            descriptionKey = "OneLife.Contains.Row2.Description", descriptionFallback = "About 960 months." },
        new LifeItem { numberKey = "OneLife.Contains.Row3.Number", numberFallback = "1,000",
            descriptionKey = "OneLife.Contains.Row3.Description",
            descriptionFallback = "About 1,000 full moons. A thousand nights when the moon returned, whether anyone noticed or not." },
        new LifeItem { numberKey = "OneLife.Contains.Row4.Number", numberFallback = "4,000",
            descriptionKey = "OneLife.Contains.Row4.Description",
            descriptionFallback = "About 4,000 weekends. The days you hoped would keep coming forever." },
        new LifeItem { numberKey = "OneLife.Contains.Row5.Number", numberFallback = "7,000",
            descriptionKey = "OneLife.Contains.Row5.Description",
            descriptionFallback = "About 7,000 songs heard more than once. Songs from childhood. Songs from kitchens, radios, dances, funerals, and barracks." },
        new LifeItem { numberKey = "OneLife.Contains.Row6.Number", numberFallback = "10,000",
            descriptionKey = "OneLife.Contains.Row6.Description",
            descriptionFallback = "Around 10,000 people you meet during your days on Earth. Friends, strangers, teachers, neighbors, colleagues, enemies and lovers." },
        new LifeItem { numberKey = "OneLife.Contains.Row7.Number", numberFallback = "15,000",
            descriptionKey = "OneLife.Contains.Row7.Description",
            descriptionFallback = "About 15,000 conversations. Some forgotten immediately. Some replayed in your head for decades." },
        new LifeItem { numberKey = "OneLife.Contains.Row8.Number", numberFallback = "20,000",
            descriptionKey = "OneLife.Contains.Row8.Description",
            descriptionFallback = "About 20,000 walks from one place to another. To school. To work. To the shop. To someone waiting." },
        new LifeItem { numberKey = "OneLife.Contains.Row9.Number", numberFallback = "29,000",
            descriptionKey = "OneLife.Contains.Row9.Description",
            descriptionFallback = "About 29,000 dinners. Meals at the kitchen table. Quick bites between errands. Celebrations at restaurants. Something greasy after a night out with friends. A nice, hearty meal with someone you loved." },
    };

    private static readonly CompareItem[] CompareItems =
    {
        new CompareItem { numberKey = "OneLife.Bars.Bar1.Number", numberFallback = "30",
            captionKey = "OneLife.Bars.Bar1.Caption",
            captionFallback = "30 dead is a classroom. The number of children who once sat around you while you learned to read, whispered jokes, and waited for the bell.",
            value = 30f, tint = new Color(0.949f, 0.925f, 0.847f, 1f) },
        new CompareItem { numberKey = "OneLife.Bars.Bar2.Number", numberFallback = "80",
            captionKey = "OneLife.Bars.Bar2.Caption",
            captionFallback = "80 dead is a crowded city bus. The people pressed around you on a rainy morning, all going somewhere ordinary.",
            value = 80f, tint = new Color(0.941f, 0.941f, 0.753f, 1f) },
        new CompareItem { numberKey = "OneLife.Bars.Bar3.Number", numberFallback = "250",
            captionKey = "OneLife.Bars.Bar3.Caption",
            captionFallback = "250 dead is a small cinema audience. The room you once shared with strangers while watching the film you loved as a child.",
            value = 250f, tint = new Color(0.812f, 0.941f, 0.682f, 1f) },
        new CompareItem { numberKey = "OneLife.Bars.Bar4.Number", numberFallback = "300",
            captionKey = "OneLife.Bars.Bar4.Caption",
            captionFallback = "300 dead is a full passenger plane. Everyone who boarded, put bags overhead, fastened seatbelts, and expected to land.",
            value = 300f, tint = new Color(0.682f, 0.941f, 0.827f, 1f) },
        new CompareItem { numberKey = "OneLife.Bars.Bar5.Number", numberFallback = "1,000",
            captionKey = "OneLife.Bars.Bar5.Caption",
            captionFallback = "1,000 dead is an entire school. Every student in the hallway. Every teacher behind a classroom door. Everyone inside it.",
            value = 1000f, tint = new Color(0.812f, 0.812f, 0.961f, 1f) },
        new CompareItem { numberKey = "OneLife.Bars.Bar6.Number", numberFallback = "5,000",
            captionKey = "OneLife.Bars.Bar6.Caption",
            captionFallback = "5,000 dead is a concert crowd. The mass of strangers you once stood among, shoulder to shoulder, singing the same chorus.",
            value = 5000f, tint = new Color(1f, 0.753f, 0.796f, 1f) },
    };

    private const int IntroStep = 0;
    private const int Phase1FirstStep = 1;
    private static readonly int Phase1LastStep = Phase1FirstStep + LifeItems.Length - 1;
    private static readonly int Phase2FirstStep = Phase1LastStep + 1;
    private static readonly int Phase2LastStep = Phase2FirstStep + CompareItems.Length - 1;
    private static readonly int ContinueStep = Phase2LastStep + 1;

    private readonly List<Image> treemapRectImages = new List<Image>();
    private readonly List<RowRefs> listRows = new List<RowRefs>();
    private Image bigSquareOverlayImage;
    private float[] barLeftX;
    private float nextRowY;
    private RectTransform transitionContent;
    private Image transitionContentImage;

    private int currentStep = -1;
    private int previousTreemapIndex = -1;
    private int currentBarIndex = -1;
    private Coroutine timerRoutine;
    private Coroutine trackScrollRoutine;
    private Coroutine textSwapRoutine;
    private Coroutine growRoutine;
    private Coroutine continueFadeRoutine;
    private Coroutine continueTransitionRoutine;
    private bool isContinueTransitioning;

    private void Awake()
    {
        BuildTreemapRectObjects();
        BuildBigSquareOverlay();
        BuildBarObjects();
        BuildTransitionContent();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += HandleLanguageChanged;
        ResetVisualState();
        GoToStep(IntroStep);
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= HandleLanguageChanged;
        StopAllCoroutines();
        timerRoutine = null;
        trackScrollRoutine = null;
        textSwapRoutine = null;
        growRoutine = null;
        continueFadeRoutine = null;
        continueTransitionRoutine = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AdvanceManually();
    }

    private void AdvanceManually()
    {
        if (currentStep >= ContinueStep)
        {
            return;
        }

        GoToStep(currentStep + 1);
    }

    private void GoToStep(int step)
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        currentStep = step;
        ApplyStep(step);

        if (step < ContinueStep)
        {
            timerRoutine = StartCoroutine(AutoAdvanceAfterDelay());
        }
    }

    private IEnumerator AutoAdvanceAfterDelay()
    {
        bool isPhase2 = currentStep >= Phase2FirstStep && currentStep <= Phase2LastStep;
        float duration = isPhase2 ? phase2StepDuration : stepDuration;
        yield return new WaitForSeconds(duration);
        timerRoutine = null;
        AdvanceManually();
    }

    private void ApplyStep(int step)
    {
        if (step == IntroStep)
        {
            return;
        }

        if (step >= Phase1FirstStep && step <= Phase1LastStep)
        {
            if (step == Phase1FirstStep)
            {
                BeginPhase1();
            }

            RevealLifeItem(step - Phase1FirstStep);
        }
        else if (step >= Phase2FirstStep && step <= Phase2LastStep)
        {
            if (step == Phase2FirstStep)
            {
                BeginPhase2();
            }

            RevealCompareItem(step - Phase2FirstStep);
        }
        else if (step == ContinueStep)
        {
            ShowContinueButton();
        }
    }

    private void BeginPhase1()
    {
        if (introTitleText != null)
        {
            introTitleText.gameObject.SetActive(false);
        }

        if (headerText != null)
        {
            headerText.gameObject.SetActive(true);
        }

        bigSquareImage.sprite = onePersonSprite;
        bigSquareImage.color = Color.white;
        bigSquareOverlayImage.sprite = onePersonSprite;
        bigSquareOverlayImage.color = Color.white;

        if (growRoutine != null)
        {
            StopCoroutine(growRoutine);
        }

        growRoutine = StartCoroutine(GrowBigSquare());
    }

    private IEnumerator GrowBigSquare()
    {
        Vector2 start = bigSquare.sizeDelta;
        Vector2 target = new Vector2(bigSquareGrownSize, bigSquareGrownSize);
        float duration = fadeTime * 1.5f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            bigSquare.sizeDelta = Vector2.Lerp(start, target, t / duration);
            yield return null;
        }

        bigSquare.sizeDelta = target;
    }

    private void RevealLifeItem(int index)
    {
        if (previousTreemapIndex >= 0 && previousTreemapIndex < treemapRectImages.Count)
        {
            treemapRectImages[previousTreemapIndex].color = PastItemColor;
        }

        Image current = treemapRectImages[index];
        current.color = CurrentItemColor;
        current.gameObject.SetActive(true);
        previousTreemapIndex = index;

        AppendListRow(index);
    }

    private void AppendListRow(int index)
    {
        LifeItem item = LifeItems[index];
        string numberValue = LocalizationManager.GetText(item.numberKey, item.numberFallback);
        string descriptionValue = LocalizationManager.GetText(item.descriptionKey, item.descriptionFallback);

        float containerWidth = listContainer.rect.width;
        const float numberWidth = 110f;
        const float columnSpacing = 20f;
        float descriptionWidth = Mathf.Max(100f, containerWidth - numberWidth - columnSpacing);

        RectTransform rowRT = CreateUIObject("Row" + (index + 1), listContainer);
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(0f, 1f);
        rowRT.pivot = new Vector2(0f, 1f);

        TextMeshProUGUI numberText = CreateText("Number", rowRT, numberFont, 24f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        RectTransform numberRT = numberText.rectTransform;
        numberRT.anchorMin = new Vector2(0f, 1f);
        numberRT.anchorMax = new Vector2(0f, 1f);
        numberRT.pivot = new Vector2(0f, 1f);
        numberRT.sizeDelta = new Vector2(numberWidth, 200f);
        numberRT.anchoredPosition = Vector2.zero;
        numberText.enableWordWrapping = false;
        numberText.color = Color.white;
        numberText.text = numberValue;

        TextMeshProUGUI descriptionText = CreateText("Description", rowRT, bodyFont, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        descriptionText.enableWordWrapping = true;
        descriptionText.color = new Color(1f, 1f, 1f, 0.92f);
        Vector2 preferred = descriptionText.GetPreferredValues(descriptionValue, descriptionWidth, 0f);
        float descriptionHeight = Mathf.Max(preferred.y, 24f);

        RectTransform descriptionRT = descriptionText.rectTransform;
        descriptionRT.anchorMin = new Vector2(0f, 1f);
        descriptionRT.anchorMax = new Vector2(0f, 1f);
        descriptionRT.pivot = new Vector2(0f, 1f);
        descriptionRT.sizeDelta = new Vector2(descriptionWidth, descriptionHeight);
        descriptionRT.anchoredPosition = new Vector2(numberWidth + columnSpacing, 0f);
        descriptionText.text = descriptionValue;

        float rowHeight = Mathf.Max(descriptionHeight, 30f);
        rowRT.sizeDelta = new Vector2(containerWidth, rowHeight);
        rowRT.anchoredPosition = new Vector2(0f, -nextRowY);
        nextRowY += rowHeight + rowSpacing;

        listRows.Add(new RowRefs
        {
            root = rowRT.gameObject,
            numberText = numberText,
            descriptionText = descriptionText,
            itemIndex = index
        });
    }

    private void BeginPhase2()
    {
        phase1Root.gameObject.SetActive(false);
        phase2Root.gameObject.SetActive(true);
    }

    private void RevealCompareItem(int index)
    {
        currentBarIndex = index;

        if (trackScrollRoutine != null)
        {
            StopCoroutine(trackScrollRoutine);
        }

        if (textSwapRoutine != null)
        {
            StopCoroutine(textSwapRoutine);
            textSwapRoutine = null;
        }

        trackScrollRoutine = StartCoroutine(ScrollTrackTo(index));
    }

    private IEnumerator ScrollTrackTo(int index)
    {
        float targetX = -barLeftX[index];
        yield return AnimateScrollX(barsTrack, targetX, barScrollSpeed, barScrollRampTime,
            compareTextSwapProgress, () => TriggerCompareTextSwap(index));
    }

    private void TriggerCompareTextSwap(int index)
    {
        if (textSwapRoutine != null)
        {
            StopCoroutine(textSwapRoutine);
        }

        textSwapRoutine = StartCoroutine(SwapCompareTextRoutine(index));
    }

    private IEnumerator SwapCompareTextRoutine(int index)
    {
        CompareItem item = CompareItems[index];
        string numberValue = LocalizationManager.GetText(item.numberKey, item.numberFallback);
        string captionValue = LocalizationManager.GetText(item.captionKey, item.captionFallback);

        yield return FadeCompareText(0f, compareTextFadeTime);

        if (currentBarNumberText != null)
        {
            currentBarNumberText.text = numberValue;
        }

        if (currentBarCaptionText != null)
        {
            currentBarCaptionText.text = captionValue;
        }

        yield return FadeCompareText(1f, compareTextFadeTime);
        textSwapRoutine = null;
    }

    private IEnumerator FadeCompareText(float targetAlpha, float duration)
    {
        float startNumberAlpha = currentBarNumberText != null ? currentBarNumberText.alpha : targetAlpha;
        float startCaptionAlpha = currentBarCaptionText != null ? currentBarCaptionText.alpha : targetAlpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);

            if (currentBarNumberText != null)
            {
                currentBarNumberText.alpha = Mathf.Lerp(startNumberAlpha, targetAlpha, progress);
            }

            if (currentBarCaptionText != null)
            {
                currentBarCaptionText.alpha = Mathf.Lerp(startCaptionAlpha, targetAlpha, progress);
            }

            yield return null;
        }

        if (currentBarNumberText != null)
        {
            currentBarNumberText.alpha = targetAlpha;
        }

        if (currentBarCaptionText != null)
        {
            currentBarCaptionText.alpha = targetAlpha;
        }
    }

    private IEnumerator AnimateScrollX(RectTransform rt, float targetX, float speedParam, float rampTimeParam,
        float triggerProgress = -1f, Action onTrigger = null)
    {
        float startX = rt.anchoredPosition.x;
        float direction = Mathf.Sign(targetX - startX);
        float distance = Mathf.Abs(targetX - startX);

        float rampTime = Mathf.Max(rampTimeParam, 0.0001f);
        float speed = Mathf.Max(speedParam, 1f);
        float rampDistance = speed * rampTime;

        float peakSpeed;
        float accelTime;
        float decelTime;
        float cruiseTime;

        if (distance >= rampDistance)
        {
            peakSpeed = speed;
            accelTime = rampTime;
            decelTime = rampTime;
            cruiseTime = (distance - rampDistance) / speed;
        }
        else
        {
            peakSpeed = distance / rampTime;
            accelTime = rampTime;
            decelTime = rampTime;
            cruiseTime = 0f;
        }

        float totalTime = accelTime + cruiseTime + decelTime;
        float accelDistance = 0.5f * peakSpeed * accelTime;
        float cruiseDistance = peakSpeed * cruiseTime;
        float t = 0f;
        bool triggered = triggerProgress < 0f;

        while (t < totalTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Min(t, totalTime);
            float travelled = TrapezoidTravelled(clampedT, accelTime, cruiseTime, decelTime, peakSpeed, accelDistance, cruiseDistance);

            float x = startX + direction * travelled;
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);

            if (!triggered)
            {
                float progress = distance > 0f ? travelled / distance : 1f;
                if (progress >= triggerProgress)
                {
                    triggered = true;
                    onTrigger?.Invoke();
                }
            }

            yield return null;
        }

        rt.anchoredPosition = new Vector2(targetX, rt.anchoredPosition.y);

        if (!triggered)
        {
            onTrigger?.Invoke();
        }
    }

    private static float TrapezoidTravelled(float clampedT, float accelTime, float cruiseTime, float decelTime, float peakSpeed, float accelDistance, float cruiseDistance)
    {
        if (clampedT <= accelTime)
        {
            return 0.5f * (peakSpeed / accelTime) * clampedT * clampedT;
        }

        if (clampedT <= accelTime + cruiseTime)
        {
            return accelDistance + peakSpeed * (clampedT - accelTime);
        }

        float t2 = clampedT - accelTime - cruiseTime;
        return accelDistance + cruiseDistance + peakSpeed * t2 - 0.5f * (peakSpeed / decelTime) * t2 * t2;
    }

    private void ShowContinueButton()
    {
        if (continueButtonGroup == null)
        {
            return;
        }

        continueButtonGroup.interactable = true;
        continueButtonGroup.blocksRaycasts = true;

        if (continueFadeRoutine != null)
        {
            StopCoroutine(continueFadeRoutine);
        }

        continueFadeRoutine = StartCoroutine(FadeCanvasGroup(continueButtonGroup, 0f, 1f, fadeTime));
    }

    public void BeginContinueTransition()
    {
        if (isContinueTransitioning)
        {
            return;
        }

        isContinueTransitioning = true;

        if (continueButtonGroup != null)
        {
            continueButtonGroup.interactable = false;
            continueButtonGroup.blocksRaycasts = false;
        }

        continueTransitionRoutine = StartCoroutine(ContinueTransitionRoutine());
    }

    private IEnumerator ContinueTransitionRoutine()
    {
        int lastIndex = CompareItems.Length - 1;
        RectTransform root = (RectTransform)transform;

        float canvasWidth = root.rect.width;
        float canvasHeight = root.rect.height;
        float viewportWidth = barsViewport.rect.width;
        float viewportHeight = barsViewport.rect.height;
        float verticalInset = (viewportHeight - barHeight) / 2f;

        float barLeftEdgeX = canvasWidth / 2f - viewportWidth / 2f;

        transitionSquare.anchorMin = new Vector2(0f, 1f);
        transitionSquare.anchorMax = new Vector2(0f, 1f);
        transitionSquare.pivot = new Vector2(0f, 1f);
        transitionSquare.anchoredPosition = new Vector2(barLeftEdgeX, barsViewport.anchoredPosition.y - verticalInset);
        transitionSquare.sizeDelta = new Vector2(viewportWidth, barHeight);
        transitionSquare.gameObject.SetActive(true);

        float contentWidth = canvasWidth + barScrollSpeed * continueGrowDuration + canvasWidth * 0.25f;
        float contentHeight = canvasHeight + barHeight;
        transitionContent.sizeDelta = new Vector2(contentWidth, contentHeight);
        transitionContentImage.sprite = manyPeopleSprite;
        transitionContentImage.color = CompareItems[lastIndex].tint;

        float contentCanvasX = 0f;
        const float contentCanvasY = 0f;
        transitionContent.anchoredPosition = new Vector2(
            contentCanvasX - transitionSquare.anchoredPosition.x,
            contentCanvasY - transitionSquare.anchoredPosition.y);

        phase2Root.gameObject.SetActive(false);

        Vector2 startPos = transitionSquare.anchoredPosition;
        Vector2 startSize = transitionSquare.sizeDelta;
        Vector2 endPos = Vector2.zero;
        Vector2 endSize = new Vector2(canvasWidth, canvasHeight);
        float duration = Mathf.Max(continueGrowDuration, 0.0001f);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Min(t, duration) / duration);

            transitionSquare.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            transitionSquare.sizeDelta = Vector2.Lerp(startSize, endSize, progress);
            SetBlackoutAlpha(progress);

            contentCanvasX -= barScrollSpeed * Time.deltaTime;
            transitionContent.anchoredPosition = new Vector2(
                contentCanvasX - transitionSquare.anchoredPosition.x,
                contentCanvasY - transitionSquare.anchoredPosition.y);

            yield return null;
        }

        transitionSquare.anchoredPosition = endPos;
        transitionSquare.sizeDelta = endSize;
        SetBlackoutAlpha(1f);

        yield return new WaitForSeconds(blackoutHoldDuration);

        if (menuManager != null)
        {
            menuManager.ContinueFromOneLifeToCredits();
        }
    }

    private void SetBlackoutAlpha(float alpha)
    {
        if (transitionBlackout == null)
        {
            return;
        }

        Color c = transitionBlackout.color;
        c.a = alpha;
        transitionBlackout.color = c;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        group.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        group.alpha = to;
    }

    private void HandleLanguageChanged(string language)
    {
        foreach (RowRefs row in listRows)
        {
            LifeItem item = LifeItems[row.itemIndex];
            row.numberText.text = LocalizationManager.GetText(item.numberKey, item.numberFallback);
            row.descriptionText.text = LocalizationManager.GetText(item.descriptionKey, item.descriptionFallback);
        }

        if (currentBarIndex >= 0 && currentBarIndex < CompareItems.Length)
        {
            CompareItem item = CompareItems[currentBarIndex];

            if (currentBarNumberText != null)
            {
                currentBarNumberText.text = LocalizationManager.GetText(item.numberKey, item.numberFallback);
            }

            if (currentBarCaptionText != null)
            {
                currentBarCaptionText.text = LocalizationManager.GetText(item.captionKey, item.captionFallback);
            }
        }
    }

    private void ResetVisualState()
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        foreach (RowRefs row in listRows)
        {
            if (row.root != null)
            {
                Destroy(row.root);
            }
        }

        listRows.Clear();
        nextRowY = 0f;

        for (int i = 0; i < treemapRectImages.Count; i++)
        {
            treemapRectImages[i].gameObject.SetActive(false);
            treemapRectImages[i].color = PastItemColor;
        }

        previousTreemapIndex = -1;

        bigSquare.sizeDelta = new Vector2(bigSquareIntroSize, bigSquareIntroSize);
        bigSquareImage.sprite = null;
        bigSquareImage.color = IntroSquareColor;
        bigSquareOverlayImage.sprite = null;
        bigSquareOverlayImage.color = IntroSquareColor;

        if (introTitleText != null)
        {
            introTitleText.gameObject.SetActive(true);
        }

        if (headerText != null)
        {
            headerText.gameObject.SetActive(false);
        }

        phase1Root.gameObject.SetActive(true);
        phase2Root.gameObject.SetActive(false);

        barsTrack.anchoredPosition = Vector2.zero;
        currentBarIndex = -1;

        if (textSwapRoutine != null)
        {
            StopCoroutine(textSwapRoutine);
            textSwapRoutine = null;
        }

        if (currentBarNumberText != null)
        {
            currentBarNumberText.text = string.Empty;
            currentBarNumberText.alpha = 1f;
        }

        if (currentBarCaptionText != null)
        {
            currentBarCaptionText.text = string.Empty;
            currentBarCaptionText.alpha = 1f;
        }

        if (continueButtonGroup != null)
        {
            continueButtonGroup.alpha = 0f;
            continueButtonGroup.interactable = false;
            continueButtonGroup.blocksRaycasts = false;
        }

        if (continueTransitionRoutine != null)
        {
            StopCoroutine(continueTransitionRoutine);
            continueTransitionRoutine = null;
        }

        isContinueTransitioning = false;

        if (transitionSquare != null)
        {
            transitionSquare.gameObject.SetActive(false);
        }

        SetBlackoutAlpha(0f);

        currentStep = -1;
    }

    private void BuildTreemapRectObjects()
    {
        for (int i = 0; i < treemapRectSlots.Length; i++)
        {
            RectTransform rt = treemapRectSlots[i];

            Image img = rt.GetComponent<Image>();
            if (img == null)
            {
                img = rt.gameObject.AddComponent<Image>();
            }

            img.raycastTarget = false;
            img.color = PastItemColor;
            rt.gameObject.SetActive(false);

            treemapRectImages.Add(img);
        }
    }

    private void BuildBigSquareOverlay()
    {
        RectTransform rt = CreateUIObject("BigSquareOverlay", bigSquare);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        bigSquareOverlayImage = rt.gameObject.AddComponent<Image>();
        bigSquareOverlayImage.raycastTarget = false;
        bigSquareOverlayImage.color = IntroSquareColor;
    }

    private void BuildBarObjects()
    {
        if (barsViewport.GetComponent<RectMask2D>() == null)
        {
            barsViewport.gameObject.AddComponent<RectMask2D>();
        }

        barLeftX = new float[CompareItems.Length];
        float x = 0f;
        float baseValue = CompareItems[0].value;

        for (int i = 0; i < CompareItems.Length; i++)
        {
            float width = barHeight * (CompareItems[i].value / baseValue);

            RectTransform rt = CreateUIObject("Bar" + (i + 1), barsTrack);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(width, barHeight);
            rt.anchoredPosition = new Vector2(x, 0f);

            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = manyPeopleSprite;
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;
            img.color = CompareItems[i].tint;

            barLeftX[i] = x;
            x += width + barGap;
        }

        barsTrack.sizeDelta = new Vector2(x, barHeight);
    }

    private void BuildTransitionContent()
    {
        if (transitionSquare.GetComponent<RectMask2D>() == null)
        {
            transitionSquare.gameObject.AddComponent<RectMask2D>();
        }

        if (transitionSquareImage != null)
        {
            transitionSquareImage.enabled = false;
        }

        transitionContent = CreateUIObject("TransitionContent", transitionSquare);
        transitionContent.anchorMin = new Vector2(0f, 1f);
        transitionContent.anchorMax = new Vector2(0f, 1f);
        transitionContent.pivot = new Vector2(0f, 1f);

        transitionContentImage = transitionContent.gameObject.AddComponent<Image>();
        transitionContentImage.type = Image.Type.Tiled;
        transitionContentImage.raycastTarget = false;
    }

    private RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        RectTransform rt = CreateUIObject(name, parent);
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();

        if (font != null)
        {
            text.font = font;
        }

        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
