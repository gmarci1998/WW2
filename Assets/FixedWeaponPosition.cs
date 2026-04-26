using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FixedWeaponPosition : MonoBehaviour
{
    [SerializeField] private float reloadDownTime = 0.6f;
    [SerializeField] private float reloadTiltTime = 1.4f;
    [SerializeField] private float reloadUpTime = 0.6f;
    [SerializeField] private float normalWeaponDownTiltAngle = 8f;
    [SerializeField] private float normalWeaponUpTiltAngle = 6f;
    [SerializeField] private float reloadTiltAngle = 50f;
    [SerializeField] private float reloadSlideDistance = -0.35f;
    [SerializeField, Range(0.05f, 0.8f)] private float reloadTiltOutPortion = 0.25f;
    [SerializeField, Range(0.05f, 0.8f)] private float reloadSlideBackPortion = 0.25f;
    [SerializeField, Range(0.05f, 0.8f)] private float reloadSlideForwardPortion = 0.25f;
    [SerializeField] private float reloadHoldTime = 1.5f;
    [SerializeField] private float reloadHiddenOffset = -4f;
    [SerializeField] private float reloadVisibleOffset = -2.75f;
    [SerializeField] private float reloadSpriteScaleMultiplier = 1.75f;

    [SerializeField] private Sprite hungarianWeaponRest;
    [SerializeField] private Sprite russianWeaponRest;
    [SerializeField] private float restDelayBeforeShow = 5f;
    [SerializeField] private float restEnterTime = 0.65f;
    [SerializeField] private float restExitTime = 0.45f;
    [SerializeField] private Vector3 restPivotOffset = new Vector3(1.15f, -0.55f, 0f);
    [SerializeField] private Vector3 restHiddenOffset = new Vector3(-1.15f, -3.1f, 0f);
    [SerializeField] private float restEnterAngle = 55f;
    [SerializeField] private float restExitAngle = -55f;
    [SerializeField] private float restSwayAngle = 2.5f;
    [SerializeField] private float restSwaySpeed = 1.35f;
    [SerializeField] private float restSpriteScaleMultiplier = 1.75f;
    [SerializeField] private Vector3 restSpritePivotOffsetTweak = Vector3.zero;

    private enum ReloadPhase { None, OriginalDown, ReloadUp, ReloadTilt, ReloadDown, OriginalUp }
    private enum NormalHideTransition { None, Lowering, Raising }
    private enum RestWeaponState { Hidden, Waiting, Entering, Idle, Exiting }

    [SerializeField]
    private ReloadPhase reloadPhase = ReloadPhase.None;
    [SerializeField]
    private NormalHideTransition normalHideTransition = NormalHideTransition.Lowering;
    [SerializeField]
    private RestWeaponState restWeaponState = RestWeaponState.Hidden;

    private float phaseTimer = 0f;
    private float normalHideTimer = 0f;
    private float normalHideStartProgress = 0f;
    private float normalHideTargetProgress = 0f;
    private float restStateTimer = 0f;
    private bool tiltStarted = false;
    private bool weaponLowerSoundPlayed = false;
    private bool weaponRaiseSoundPlayed = false;
    private bool wasHiding = false;

    public static FixedWeaponPosition Instance { get; private set; }

    [SerializeField] private Camera cam;
    [SerializeField] private Vector3 viewportOffset = new Vector3(0.5f, 0.2f, 10f);
    [SerializeField] private GameObject fireSpark;
    [SerializeField] private AudioSource fireSound;
    [SerializeField] private AudioSource reloadSound;
    [SerializeField] private AudioSource weaponLowerSound;
    [SerializeField] private AudioSource weaponRaiseSound;
    [SerializeField] private Animator sparkAnimator;

    private List<Transform> enemySoldiers = new List<Transform>();
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer restSpriteRenderer;

    [SerializeField] private Sprite hungarianReloadWeapon;
    [SerializeField] private Sprite russianReloadWeapon;
    [SerializeField] private Sprite hungarianWeapon;
    [SerializeField] private Sprite russianWeapon;
    [SerializeField] private float lerpSpeed = 4f;

    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeMagnitude = 0.08f;

    [SerializeField] private float kickbackScale = 1.15f;
    [SerializeField] private float kickbackDuration = 0.08f;

    private bool isActive = false;
    private bool isReloading = false;

    private float startY = -3f;
    private float targetY = -7f;
    private float reloadVisibleY;
    private float initialCameraY;
    private Vector3 initialWeaponPosition;
    private float currentReloadSlideOffset = 0f;
    private float currentHideProgress = 0f;

    private Vector3 originalScale;
    private bool isKickbacking = false;

    List<Transform> GetEnemies => FindObjectsByType<SoldierMovement>(FindObjectsSortMode.None).Select(s => s.transform).ToList();

    void Awake()
    {
        Instance = this;
        //TODO: maybe only if starting in hiding state?

    }

    void Start()
{
    if (cam == null) cam = Camera.main;
    enemySoldiers = GameManager.Instance.GetEnemies();
    spriteRenderer = GetComponent<SpriteRenderer>();
    originalScale = transform.localScale;
    initialWeaponPosition = transform.position;
    initialCameraY = cam != null ? cam.transform.position.y : 0f;
    startY = initialWeaponPosition.y;
    RefreshReloadPositions();
    EnsureRestSpriteRenderer();
    SetRestSpriteVisible(false);
    
    BeginRestEnter();

    // Módosítás: a kezdeti állapot pontos beállítása
    wasHiding = GameManager.Instance != null && GameManager.Instance.IsHiding();
    
    if (wasHiding)
    {
        // Ha bújik, azonnal a rejtett pozícióba tesszük (kikerüljük az animációt induláskor)
        SetNormalWeaponHiddenPose(GetCameraYOffset());
        HideNormalWeaponSprite();
        currentHideProgress = 1f; // A progress 1, tehát rejtve vagyunk
    }
    else
    {
        currentHideProgress = 0f; // Látható
    }
}

    void Update()
    {
        bool isCurrentlyHiding = GameManager.Instance != null && GameManager.Instance.IsHiding();
        Debug.Log("IsHiding: " + isCurrentlyHiding );

        if (Input.GetMouseButtonDown(0) && !isActive && GameManager.Instance.CanShoot())
        {
            Debug.Log("!isActive: " + !isActive + " CanShoot: " + GameManager.Instance.CanShoot() + " CanvasAlpha: " + GameManager.Instance.canvas_opening.GetComponent<CanvasGroup>().alpha);
            if (!isActive && GameManager.Instance.CanShoot() && GameManager.Instance.canvas_opening.GetComponent<CanvasGroup>().alpha == 0f)
            {
                StartCoroutine(FireEffect());
                CheckIfEnemyGotShot();
            }
        }

        if (cam == null) cam = Camera.main;
        RefreshReloadPositions();
        float weaponYOffset = GetCameraYOffset();

        if (!isReloading)
        {
            ApplyNormalWeaponVisuals();
            HandleHideStateTransition(isCurrentlyHiding);
        }
        else
        {
            wasHiding = isCurrentlyHiding;
        }

        if (isReloading)
        {
            UpdateReloadState(weaponYOffset);
            return;
        }

        if (normalHideTransition != NormalHideTransition.None)
        {
            UpdateNormalHideTransition(weaponYOffset, isCurrentlyHiding);
            return;
        }

        if (restWeaponState != RestWeaponState.Hidden)
        {
            UpdateRestWeaponState(weaponYOffset, isCurrentlyHiding);
            return;
        }

        if (isCurrentlyHiding)
        {
            HideNormalWeaponSprite();
            SetRestSpriteVisible(false);
            SetNormalWeaponHiddenPose(weaponYOffset);
            return;
        }

        ShowNormalWeaponSprite();
        SetRestSpriteVisible(false);
        transform.position = new Vector3(
            initialWeaponPosition.x,
            startY + weaponYOffset,
            initialWeaponPosition.z
        );
        transform.localRotation = Quaternion.identity;
    }

    void UpdateReloadState(float weaponYOffset)
    {
        ShowNormalWeaponSprite();
        SetRestSpriteVisible(false);

        Vector3 pos = transform.position;
        Vector3 slideOffset = transform.right * currentReloadSlideOffset;
        pos.x = initialWeaponPosition.x + slideOffset.x;

        switch (reloadPhase)
        {
            case ReloadPhase.OriginalDown:
            {
                ApplyNormalWeaponVisuals();

                if (!weaponLowerSoundPlayed)
                {
                    PlayWeaponLowerSound();
                    weaponLowerSoundPlayed = true;
                }

                float progress = phaseTimer / reloadDownTime;
                pos.y = Mathf.Lerp(startY, targetY, progress) + weaponYOffset + slideOffset.y;
                transform.position = pos;
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, normalWeaponDownTiltAngle, progress));

                phaseTimer += Time.deltaTime * lerpSpeed;

                if (phaseTimer >= reloadDownTime)
                {
                    phaseTimer = 0f;
                    reloadPhase = ReloadPhase.ReloadUp;
                }
                break;
            }

            case ReloadPhase.ReloadUp:
            {
                ApplyReloadWeaponVisuals();

                float progress = phaseTimer / reloadUpTime;
                pos.y = Mathf.Lerp(targetY, reloadVisibleY, progress) + weaponYOffset + slideOffset.y;
                transform.position = pos;

                phaseTimer += Time.deltaTime * lerpSpeed;

                if (phaseTimer >= reloadUpTime)
                {
                    phaseTimer = 0f;
                    reloadPhase = ReloadPhase.ReloadTilt;
                    tiltStarted = false;
                }
                break;
            }

            case ReloadPhase.ReloadTilt:
            {
                ApplyReloadWeaponVisuals();

                pos.y = reloadVisibleY + weaponYOffset + slideOffset.y;
                transform.position = pos;

                if (!tiltStarted)
                {
                    tiltStarted = true;
                    StartCoroutine(TiltWeapon());
                }

                phaseTimer += Time.deltaTime * lerpSpeed;

                if (phaseTimer >= reloadTiltTime)
                {
                    phaseTimer = 0f;
                    reloadPhase = ReloadPhase.ReloadDown;
                }
                break;
            }

            case ReloadPhase.ReloadDown:
            {
                ApplyReloadWeaponVisuals();

                float progress = phaseTimer / reloadDownTime;
                pos.y = Mathf.Lerp(reloadVisibleY, targetY, progress) + weaponYOffset + slideOffset.y;
                transform.position = pos;

                phaseTimer += Time.deltaTime * lerpSpeed;

                if (phaseTimer >= reloadDownTime)
                {
                    phaseTimer = 0f;
                    reloadPhase = ReloadPhase.OriginalUp;
                }
                break;
            }

            case ReloadPhase.OriginalUp:
            {
                // Check if the player is hiding before returning to the original state
                if (GameManager.Instance != null && GameManager.Instance.IsHiding())
                {
                    reloadPhase = ReloadPhase.None;
                    isReloading = false;
                    isActive = false;
                    
                    // Kényszerítsük a rejtett pozíciót, hogy ne legyen "ugrás"
                    currentHideProgress = 1f; 
                    SetNormalWeaponHiddenPose(weaponYOffset);
                    HideNormalWeaponSprite();
                    BeginRestWait();
                    return;
                }

                ApplyNormalWeaponVisuals();

                if (!weaponRaiseSoundPlayed)
                {
                    PlayWeaponRaiseSound();
                    weaponRaiseSoundPlayed = true;
                }

                float progress = phaseTimer / reloadUpTime;
                pos.y = Mathf.Lerp(targetY, startY, progress) + weaponYOffset + slideOffset.y;
                transform.position = pos;
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(normalWeaponUpTiltAngle, 0f, progress));

                phaseTimer += Time.deltaTime * lerpSpeed;

                if (phaseTimer >= reloadUpTime)
                {
                    phaseTimer = 0f;
                    reloadPhase = ReloadPhase.None;
                    isReloading = false;
                    isActive = false;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = originalScale;
                    currentReloadSlideOffset = 0f;
                }
                break;
            }
        }
    }

    IEnumerator TiltWeapon()
    {
        Quaternion originalRot = transform.localRotation;
        Quaternion tiltRot = originalRot * Quaternion.Euler(0, 0, reloadTiltAngle);
        float tiltOutPortion = reloadTiltOutPortion;
        float slideBackPortion = reloadSlideBackPortion;
        float slideForwardPortion = reloadSlideForwardPortion;
        float usedPortion = tiltOutPortion + slideBackPortion + slideForwardPortion;

        if (usedPortion > 0.95f)
        {
            float normalizeFactor = 0.95f / usedPortion;
            tiltOutPortion *= normalizeFactor;
            slideBackPortion *= normalizeFactor;
            slideForwardPortion *= normalizeFactor;
        }

        float tiltBackPortion = 1f - (tiltOutPortion + slideBackPortion + slideForwardPortion);
        float tiltOutDuration = reloadTiltTime * tiltOutPortion;
        float slideBackDuration = reloadTiltTime * slideBackPortion;
        float slideForwardDuration = reloadTiltTime * slideForwardPortion;
        float tiltBackDuration = reloadTiltTime * tiltBackPortion;

        yield return RotateWeapon(originalRot, tiltRot, tiltOutDuration);
        PlayReloadSound();
        yield return SlideWeapon(0f, reloadSlideDistance, slideBackDuration);
        yield return SlideWeapon(reloadSlideDistance, 0f, slideForwardDuration);
        yield return RotateWeapon(tiltRot, originalRot, tiltBackDuration);

        currentReloadSlideOffset = 0f;
        transform.localRotation = originalRot;
    }

    IEnumerator RotateWeapon(Quaternion from, Quaternion to, float duration)
    {
        if (duration <= 0f)
        {
            transform.localRotation = to;
            yield break;
        }

        float t = 0f;

        while (t < duration)
        {
            transform.localRotation = Quaternion.Lerp(from, to, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = to;
    }

    IEnumerator SlideWeapon(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            currentReloadSlideOffset = to;
            yield break;
        }

        float t = 0f;

        while (t < duration)
        {
            currentReloadSlideOffset = Mathf.Lerp(from, to, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        currentReloadSlideOffset = to;
    }

    void HandleHideStateTransition(bool isCurrentlyHiding)
    {
        if (isCurrentlyHiding == wasHiding)
        {
            return;
        }

        wasHiding = isCurrentlyHiding;

        if (isCurrentlyHiding)
        {
            if (restWeaponState == RestWeaponState.Entering || restWeaponState == RestWeaponState.Idle || restWeaponState == RestWeaponState.Exiting)
            {
                restWeaponState = RestWeaponState.Idle;
                return;
            }

            StartNormalHideTransition(NormalHideTransition.Lowering);
            return;
        }

        if (restWeaponState == RestWeaponState.Waiting)
        {
            restWeaponState = RestWeaponState.Hidden;
            restStateTimer = 0f;
            StartNormalHideTransition(NormalHideTransition.Raising);
            return;
        }

        if (restWeaponState == RestWeaponState.Entering || restWeaponState == RestWeaponState.Idle)
        {
            BeginRestExit();
            return;
        }

        if (currentHideProgress > 0f)
        {
            StartNormalHideTransition(NormalHideTransition.Raising);
        }
    }

    void StartNormalHideTransition(NormalHideTransition transition)
    {
        normalHideTransition = transition;
        normalHideTimer = 0f;
        normalHideStartProgress = currentHideProgress;
        normalHideTargetProgress = transition == NormalHideTransition.Lowering ? 1f : 0f;

        ShowNormalWeaponSprite();
        SetRestSpriteVisible(false);
        restWeaponState = RestWeaponState.Hidden;
        restStateTimer = 0f;

        if (transition == NormalHideTransition.Lowering)
        {
            PlayWeaponLowerSound();
        }
        else
        {
            PlayWeaponRaiseSound();
        }
    }

    void UpdateNormalHideTransition(float weaponYOffset, bool isCurrentlyHiding)
    {
        ApplyNormalWeaponVisuals();
        ShowNormalWeaponSprite();
        SetRestSpriteVisible(false);

        float duration = normalHideTransition == NormalHideTransition.Lowering ? reloadDownTime : reloadUpTime;
        if (duration <= 0f)
        {
            currentHideProgress = normalHideTargetProgress;
            FinishNormalHideTransition(isCurrentlyHiding);
            return;
        }

        normalHideTimer += Time.deltaTime * lerpSpeed;
        float progress = Mathf.Clamp01(normalHideTimer / duration);
        currentHideProgress = Mathf.Lerp(normalHideStartProgress, normalHideTargetProgress, progress);

        float y = Mathf.Lerp(startY, targetY, currentHideProgress) + weaponYOffset;
        float zRotation = Mathf.Lerp(0f, normalWeaponDownTiltAngle, currentHideProgress);

        transform.position = new Vector3(
            initialWeaponPosition.x,
            y,
            initialWeaponPosition.z
        );
        transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        if (progress >= 1f)
        {
            FinishNormalHideTransition(isCurrentlyHiding);
        }
    }

    void FinishNormalHideTransition(bool isCurrentlyHiding)
    {
        NormalHideTransition finishedTransition = normalHideTransition;
        normalHideTransition = NormalHideTransition.None;
        normalHideTimer = 0f;

        if (finishedTransition == NormalHideTransition.Lowering)
        {
            HideNormalWeaponSprite();
            if (isCurrentlyHiding)
            {
                BeginRestWait();
            }
            return;
        }

        ShowNormalWeaponSprite();
        currentHideProgress = 0f;
        transform.localRotation = Quaternion.identity;
    }

    void BeginRestWait()
    {
        restWeaponState = GetCurrentRestWeaponSprite() == null ? RestWeaponState.Hidden : RestWeaponState.Waiting;
        restStateTimer = 0f;
        SetRestSpriteVisible(false);
    }

    void BeginRestEnter()
    {
        if (GetCurrentRestWeaponSprite() == null)
        {
            restWeaponState = RestWeaponState.Hidden;
            return;
        }

        UpdateRestSpriteVisuals();
        restWeaponState = RestWeaponState.Entering;
        restStateTimer = 0f;
        HideNormalWeaponSprite();
        SetRestSpriteVisible(true);
    }

    void BeginRestExit()
    {
        if (GetCurrentRestWeaponSprite() == null)
        {
            restWeaponState = RestWeaponState.Hidden;
            StartNormalHideTransition(NormalHideTransition.Raising);
            return;
        }

        UpdateRestSpriteVisuals();
        restWeaponState = RestWeaponState.Exiting;
        restStateTimer = 0f;
        HideNormalWeaponSprite();
        SetRestSpriteVisible(true);
    }

    void UpdateRestWeaponState(float weaponYOffset, bool isCurrentlyHiding)
    {
        HideNormalWeaponSprite();

        switch (restWeaponState)
        {
            case RestWeaponState.Waiting:
            {
                SetRestSpriteVisible(false);
                SetNormalWeaponHiddenPose(weaponYOffset);
                restStateTimer += Time.deltaTime;

                if (restStateTimer >= restDelayBeforeShow && isCurrentlyHiding)
                {
                    BeginRestEnter();
                }
                break;
            }

            case RestWeaponState.Entering:
            {
                SetRestSpriteVisible(true);
                restStateTimer += Time.deltaTime;
                float progress = restEnterTime <= 0f ? 1f : Mathf.Clamp01(restStateTimer / restEnterTime);

                Vector3 pivotPosition = Vector3.Lerp(GetRestHiddenPivotPosition(weaponYOffset), GetRestVisiblePivotPosition(weaponYOffset), progress);
                float zRotation = Mathf.Lerp(restEnterAngle, 0f, progress);
                SetRestWeaponPose(pivotPosition, zRotation);

                if (progress >= 1f)
                {
                    restWeaponState = RestWeaponState.Idle;
                    restStateTimer = 0f;
                }
                break;
            }

            case RestWeaponState.Idle:
            {
                SetRestSpriteVisible(true);
                float sway = Mathf.Sin(Time.time * restSwaySpeed) * restSwayAngle;
                SetRestWeaponPose(GetRestVisiblePivotPosition(weaponYOffset), sway);
                break;
            }

            case RestWeaponState.Exiting:
            {
                SetRestSpriteVisible(true);
                restStateTimer += Time.deltaTime;
                float progress = restExitTime <= 0f ? 1f : Mathf.Clamp01(restStateTimer / restExitTime);

                Vector3 pivotPosition = Vector3.Lerp(GetRestVisiblePivotPosition(weaponYOffset), GetRestHiddenPivotPosition(weaponYOffset), progress);
                float zRotation = Mathf.Lerp(0f, restExitAngle, progress);
                SetRestWeaponPose(pivotPosition, zRotation);

                if (progress >= 1f)
                {
                    restWeaponState = RestWeaponState.Hidden;
                    restStateTimer = 0f;
                    SetRestSpriteVisible(false);

                    if (!isCurrentlyHiding)
                    {
                        StartNormalHideTransition(NormalHideTransition.Raising);
                    }
                }
                break;
            }
        }
    }

    void EnsureRestSpriteRenderer()
    {
        if (restSpriteRenderer != null)
        {
            return;
        }

        Transform existingChild = transform.Find("RestWeaponSprite");
        GameObject restObject;

        if (existingChild != null)
        {
            restObject = existingChild.gameObject;
        }
        else
        {
            restObject = new GameObject("RestWeaponSprite");
            restObject.transform.SetParent(transform, false);
        }

        restSpriteRenderer = restObject.GetComponent<SpriteRenderer>();
        if (restSpriteRenderer == null)
        {
            restSpriteRenderer = restObject.AddComponent<SpriteRenderer>();
        }
    }

    void UpdateRestSpriteVisuals()
    {
        EnsureRestSpriteRenderer();
        restSpriteRenderer.sprite = GetCurrentRestWeaponSprite();
        restSpriteRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        restSpriteRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        restSpriteRenderer.color = spriteRenderer.color;
        restSpriteRenderer.flipX = spriteRenderer.flipX;
        restSpriteRenderer.flipY = spriteRenderer.flipY;
        restSpriteRenderer.transform.localScale = Vector3.one * restSpriteScaleMultiplier;
        UpdateRestSpriteAnchorOffset();
    }

    void UpdateRestSpriteAnchorOffset()
    {
        if (restSpriteRenderer == null || restSpriteRenderer.sprite == null)
        {
            return;
        }

        Vector3 childScale = restSpriteRenderer.transform.localScale;
        Vector3 extents = restSpriteRenderer.sprite.bounds.extents;
        Vector3 anchorOffset = new Vector3(
            extents.x * childScale.x,
            extents.y * childScale.y,
            0f
        );

        restSpriteRenderer.transform.localPosition = anchorOffset + restSpritePivotOffsetTweak;
        restSpriteRenderer.transform.localRotation = Quaternion.identity;
    }

    void SetRestSpriteVisible(bool isVisible)
    {
        if (restSpriteRenderer == null)
        {
            return;
        }

        restSpriteRenderer.enabled = isVisible;
    }

    void HideNormalWeaponSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    void ShowNormalWeaponSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    void SetNormalWeaponHiddenPose(float weaponYOffset)
    {
        transform.position = new Vector3(
            initialWeaponPosition.x,
            targetY + weaponYOffset,
            initialWeaponPosition.z
        );
        transform.localRotation = Quaternion.Euler(0f, 0f, normalWeaponDownTiltAngle);
    }

    Vector3 GetRestVisiblePivotPosition(float weaponYOffset)
    {
        return new Vector3(
            initialWeaponPosition.x + restPivotOffset.x,
            startY + restPivotOffset.y + weaponYOffset,
            initialWeaponPosition.z + restPivotOffset.z
        );
    }

    Vector3 GetRestHiddenPivotPosition(float weaponYOffset)
    {
        Vector3 visiblePosition = GetRestVisiblePivotPosition(weaponYOffset);
        return visiblePosition + restHiddenOffset;
    }

    void SetRestWeaponPose(Vector3 pivotPosition, float zRotation)
    {
        transform.localScale = originalScale;
        transform.position = pivotPosition;
        transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    Sprite GetCurrentRestWeaponSprite()
    {
        if (GameManager.Instance.GetSide())
        {
            return hungarianWeaponRest;
        }

        return russianWeaponRest;
    }

    void PlayReloadSound()
    {
        if (reloadSound != null)
        {
            reloadSound.Play();
        }
    }

    void PlayWeaponLowerSound()
    {
        if (weaponLowerSound != null)
        {
            weaponLowerSound.Play();
        }
    }

    void PlayWeaponRaiseSound()
    {
        if (weaponRaiseSound != null)
        {
            weaponRaiseSound.Play();
        }
    }

    float GetCameraYOffset()
    {
        if (cam == null)
        {
            return 0f;
        }

        return cam.transform.position.y - initialCameraY;
    }

    void RefreshReloadPositions()
    {
        targetY = startY + reloadHiddenOffset;
        reloadVisibleY = startY + reloadVisibleOffset;
    }

    void LateUpdate() { }

    IEnumerator FireEffect()
    {
        isActive = true;
        fireSound.Play();
        sparkAnimator.SetTrigger("Fire");

        SetRestSpriteVisible(false);
        restWeaponState = RestWeaponState.Hidden;
        normalHideTransition = NormalHideTransition.None;
        currentHideProgress = 0f;

        StartCoroutine(WeaponKickback());
        StartCoroutine(ShakeCamera());

        yield return new WaitForSeconds(0.3f);

        isReloading = true;
        reloadPhase = ReloadPhase.OriginalDown;
        phaseTimer = 0f;
        tiltStarted = false;
        weaponLowerSoundPlayed = false;
        weaponRaiseSoundPlayed = false;
    }

    IEnumerator WeaponKickback()
    {
        if (isKickbacking) yield break;
        isKickbacking = true;

        Vector3 bigScale = originalScale * kickbackScale;
        float elapsed = 0f;

        while (elapsed < kickbackDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, bigScale, elapsed / kickbackDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < kickbackDuration)
        {
            transform.localScale = Vector3.Lerp(bigScale, originalScale, elapsed / kickbackDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        isKickbacking = false;
    }

    void ApplyReloadWeaponVisuals()
    {
        transform.localScale = originalScale * reloadSpriteScaleMultiplier;
        SetReloadWeaponSprite();
    }

    void ApplyNormalWeaponVisuals()
    {
        if (!isKickbacking)
        {
            transform.localScale = originalScale;
        }

        SetNormalWeaponSprite();
    }

    void SetReloadWeaponSprite()
    {
        if (GameManager.Instance.GetSide())
        {
            spriteRenderer.sprite = hungarianReloadWeapon;
        }
        else
        {
            spriteRenderer.sprite = russianReloadWeapon;
        }
    }

    void SetNormalWeaponSprite()
    {
        if (GameManager.Instance.GetSide())
        {
            spriteRenderer.sprite = hungarianWeapon;
        }
        else
        {
            spriteRenderer.sprite = russianWeapon;
        }
    }

    public void ShakeCameraByHit()
    {
        StartCoroutine(ShakeCamera());
    }

    IEnumerator ShakeCamera()
    {
        Vector3 originalCamPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            cam.transform.localPosition = new Vector3(
                originalCamPos.x + x,
                originalCamPos.y + y,
                originalCamPos.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.transform.localPosition = originalCamPos;
    }

    void CheckIfEnemyGotShot()
    {
        foreach (Transform enemy in GetEnemies)
        {
            if (enemy == null) continue;
            var soldier = enemy.GetComponent<SoldierMovement>();
            if (soldier.IsDead || !soldier.IsKillable) continue;
            float distance = Vector3.Distance(
                new Vector3(fireSpark.transform.position.x, fireSpark.transform.position.y, 0f),
                new Vector3(enemy.transform.position.x, fireSpark.transform.position.y, 0f)
            );
            if (distance < 0.3)
            {
                Debug.Log("BUMMM");
                soldier.Die();
            }
        }
    }
}
