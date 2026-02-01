using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHead : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField] GameObject cover;
    [SerializeField] private float coverOffset = 5f;
    [SerializeField] private float coverMoveDuration = 0.15f;

    private Coroutine fadeRoutine;
    private bool isCrouching = false;
    private Vector3 coverOriginalPosition;
    private Coroutine coverMoveRoutine;

    void Start()
    {
        if (cover != null)
            coverOriginalPosition = cover.transform.position;
    }

    void Update()
    {
        // Pressing space toggles crouching
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleCrouch();
        }
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        Debug.Log($"PlayerHead: Crouch toggled -> {isCrouching} (GameObject: {gameObject.name})");

        // Move cover up/down smoothly
        if (cover != null)
        {
            if (coverMoveRoutine != null)
                StopCoroutine(coverMoveRoutine);
            coverMoveRoutine = StartCoroutine(MoveCover(isCrouching));
        }
    }

    IEnumerator MoveCover(bool toCrouch)
    {
        Vector3 start = cover.transform.position;
        Vector3 target = coverOriginalPosition + (toCrouch ? Vector3.up * coverOffset : Vector3.zero);
        float elapsed = 0f;

        while (elapsed < coverMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / coverMoveDuration);
            cover.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        cover.transform.position = target;
        coverMoveRoutine = null;
    }

    public void Hit()
    {
        // If crouching, ignore hits
        if (isCrouching)
        {
            Debug.Log($"PlayerHead: Hit ignored while crouching (GameObject: {gameObject.name})");
            return;
        }

        // The screen should fade to black and reload the level
        if (fadeRoutine == null)
        {
            fadeRoutine = StartCoroutine(FadeToBlackAndReload());
        }
    }

    IEnumerator FadeToBlackAndReload()
    {
        // Try to find an existing fader first
        GameObject faderRoot = GameObject.Find("ScreenFader");
        CanvasGroup cg = null;

        if (faderRoot != null)
        {
            cg = faderRoot.GetComponent<CanvasGroup>();
        }

        // If none found, create a full-screen black fader (Canvas + Image + CanvasGroup)
        if (cg == null)
        {
            faderRoot = new GameObject("ScreenFader");
            var canvas = faderRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            faderRoot.AddComponent<CanvasScaler>();
            faderRoot.AddComponent<GraphicRaycaster>();

            cg = faderRoot.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var imageGO = new GameObject("FaderImage");
            imageGO.transform.SetParent(faderRoot.transform, false);

            var rt = imageGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var img = imageGO.AddComponent<Image>();
            img.color = Color.black;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = 1f;

        // small pause to ensure the player sees the full black frame
        yield return new WaitForSeconds(0.15f);

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // fadeRoutine cleared by scene reload; guard in case reload is suppressed in editor
        fadeRoutine = null;
    }
}
