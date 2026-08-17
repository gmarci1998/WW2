using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour 
{
    [Header("Menu Navigation")]
    public bool showOneLifeOnGameEnd = false;
    
    [SerializeField] private Subtitles subtitles;

    [SerializeField] private AudioSource ambient;
    [SerializeField] private AudioSource wind;
    [SerializeField] private AudioSource distance;
    [SerializeField] private AudioSource radioNoiseAudio;
    [SerializeField] private AudioSource narrationPauseWarningAudio;
    [SerializeField] private float radioNoiseNormalVolume = 0.080f;
    [SerializeField] private float radioNoiseHidingVolume = 0.350f;
    [SerializeField] private float radioNoiseFadeInDuration = 5f;
    [SerializeField] private float radioNoiseFadeOutDuration = 2f;
    [SerializeField] private AudioSource standUpAudio;
    [SerializeField] private AudioSource crouchAudio;
    [SerializeField] private Camera cam;
    [SerializeField] private float parallaxFactorX = 2f;  
    [SerializeField] private float parallaxFactorY = 0.1f; 
    [SerializeField] private float maxOffsetY = 2f;     
    [SerializeField] private bool hungarianSide = true;

    [SerializeField] private SpriteRenderer background;
    [SerializeField] Sprite hungarianBackground;
    [SerializeField] Sprite russianBackground;
    
    [SerializeField] private SpriteRenderer middleground;
    [SerializeField] Sprite hungarianMiddleground;
    [SerializeField] Sprite russianMiddleground;
    [SerializeField] GameObject death;
    [SerializeField] GameObject flagHun;
    [SerializeField] GameObject flagRus;
    [SerializeField] GameObject license;

    [Tooltip("Az a fedezék Sprite Renderer-e (a \"Ground (1)\" objektum), amelyiken a kép a szovjet/magyar oldal szerint vált.")]
    [SerializeField] private SpriteRenderer ruinsCoverRenderer;
    [Tooltip("Ezt a képet mutassa a ruinsCoverRenderer szovjet oldalon (pl. ruins_1_0).")]
    [SerializeField] private Sprite sovietRuinsSprite;
    [Tooltip("Ezt a képet mutassa a ruinsCoverRenderer magyar oldalon (pl. ruins_2_0).")]
    [SerializeField] private Sprite hungarianRuinsSprite;
    [Tooltip("A sovietRuinsSprite egységes (uniform) skálája - a két kép mérete eltér, enélkül váltáskor megugrana/összezsugorodna.")]
    [SerializeField] private float sovietRuinsScale = 0.31095874f;
    [Tooltip("A hungarianRuinsSprite egységes (uniform) skálája.")]
    [SerializeField] private float hungarianRuinsScale = 1f;

    [Tooltip("A \"Fog\" objektum Sprite Renderer-e - magyar oldalon megfordítjuk a mozgása irányát.")]
    [SerializeField] private SpriteRenderer fogRenderer;
    private Vector4 fogBaseSpeed;
    private Vector4 fogBaseSpeed2;

    [Tooltip("A TopView térképen a magyar oldalhoz tartozó X jelölők szülő objektuma (gyerekei: az egyes X pozíciók).")]
    [SerializeField] private RectTransform hungarianXGroup;
    [Tooltip("A TopView térképen a szovjet oldalhoz tartozó X jelölők szülő objektuma (gyerekei: az egyes X pozíciók).")]
    [SerializeField] private RectTransform sovietXGroup;

    [Header("Exit Confirmation UI")]
    public GameObject exitConfirmationPanel;
    public Button exitConfirmButton;
    public Button exitCancelButton;
    public TextMeshProUGUI exitConfirmationTitleText;
    public TextMeshProUGUI exitConfirmationMessageText;
    public TextMeshProUGUI exitYesButtonText;
    public TextMeshProUGUI exitNoButtonText;
    public string exitConfirmationTitle = "Kilépés";
    public string exitConfirmationMessage = "Kilépsz a főmenübe?";

    [Header("Opening Caption UI")]
    public TextMeshProUGUI openingCaptionText;

    [Header("Narration Complete Transition")]
    [Tooltip("Ennyi másodperc alatt jelenik meg fokozatosan az átvezető, miután végighallgattunk egy narrációt.")]
    [SerializeField] private float narrationCompleteFadeInDuration = 3f;
    [Tooltip("Miután teljesen látszik, ennyi másodpercig marad még kint az átvezető, mielőtt a következő katonára váltanánk.")]
    [SerializeField] private float narrationCompleteHoldDuration = 3f;
    private bool narrationCompleteUIBuilt = false;
    private CanvasGroup narrationCompleteCanvasGroup;
    private Image narrationCompletePortraitImage;
    private TextMeshProUGUI narrationCompleteText;

    [SerializeField] private AudioSource playerHitAudio;
    [SerializeField] private AudioSource playerMissAudio;
    [SerializeField] private AudioSource playerDeathAudio;
    [SerializeField] private AudioSource gunshothAudio;
    [SerializeField] private AudioSource sniperGunshotAudio;


    private Vector3 startPos;
    public GameObject backgroundSprite;  
    public GameObject middlegroundSprite;
    public GameObject canvas_opening;    
    private List<Transform> enemySoldiers = new List<Transform>();
    private List<CursorMovementEnemy> enemyMovers = new List<CursorMovementEnemy>();

    
    private Vector3 licenseViewportOffset = new Vector3(0.075f, 0.1f, 8f);
    private bool licenseInitialized = false;
    private bool didSurvive = false;

    private bool isHiding = true;
    private bool hideActive = true;
    private float camStartY;           
    private float camTargetY;          
    private float camLerpT = 0f;      
    private bool camMoving = false;    
    private float camLerpSpeed = 2f;  
    private bool canShoot = true;
    private bool isPlayerDead = false; 
    [SerializeField] private int playerLives = 3;

    private bool exitConfirmationVisible = false;

    private bool isLock = false;
    private int maxPlayerLives = 3;

    [Tooltip("Ennyi másodpercig kell sértetlennek maradnia a játékosnak ahhoz, hogy plusz 1 életet kapjon.")]
    [SerializeField] private float lifeRegenDelay = 30f;
    private Coroutine lifeRegenCoroutine;

    private SpriteRenderer licenseSpriteRenderer;
    private SpriteRenderer shatterSpriteRenderer;
    private bool sideVisualsApplied = false;
    private bool appliedHungarianSide;

    public static GameManager Instance { get; private set; }
    
    [SerializeField] private SoldierData[] HungarianSoldiers;
    [SerializeField] private SoldierData[] RussianSoldiers;

    SoldierData currentSoldier;

    private AudioSource narrationAudio;
    private float narrationPauseTime;
    private Coroutine startNarrationCoroutine;
    private Coroutine stopNarrationCoroutine;
    private Coroutine resumeNarrationCoroutine;
    [SerializeField] private float narrationPauseDelay = 8f;
    private bool previousIsHiding;
    private float radioNoiseTargetVolume;

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
        public string NarrationLanguage; // Új mező a narráció nyelvéhez
        public bool SubtitlesEnabled;   // Új mező a feliratok engedélyezéséhez
        public string SubtitlesLanguage; // Új mező a feliratok nyelvéhez
    }

    public void TakeDamage(int damage)
    {
        if (isPlayerDead) return;

        playerLives -= damage;// TODO - delete this

        SetShatterPortraitAlpha(playerLives);
        PlayPlayerHitSound();
        if (FixedWeaponPosition.Instance != null){
            FixedWeaponPosition.Instance.ShakeCameraByHit();
        }

        //Debug.Log("Player élete: " + playerLives);

        if (playerLives <= 0)
        {
            PlayerDeath();
            return;
        }

        if (lifeRegenCoroutine != null)
        {
            StopCoroutine(lifeRegenCoroutine);
        }
        lifeRegenCoroutine = StartCoroutine(RegenerateLifeAfterDelay());
    }

    IEnumerator RegenerateLifeAfterDelay()
    {
        float elapsed = 0f;

        while (elapsed < lifeRegenDelay)
        {
            if (!isHiding)
            {
                elapsed += Time.deltaTime;
            }

            yield return null;
        }

        lifeRegenCoroutine = null;

        if (isPlayerDead || playerLives >= maxPlayerLives)
        {
            yield break;
        }

        playerLives++;
        StartCoroutine(PlayLifeRegenPortraitAnimation());
    }

    IEnumerator PlayLifeRegenPortraitAnimation()
    {
        if (shatterSpriteRenderer == null)
        {
            shatterSpriteRenderer = license.transform.GetChild(0).GetComponent<SpriteRenderer>();
        }

        float startAlpha = shatterSpriteRenderer.color.a;
        float targetAlpha = playerLives == maxPlayerLives
            ? 0f
            : 1f / (System.Math.Max(playerLives, 0) + 1f);

        Transform portraitTransform = license.transform;
        Vector3 baseScale = portraitTransform.localScale;
        Vector3 pulseScale = baseScale * 1.15f;

        float pulseDuration = 0.3f;
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pulseDuration);

            portraitTransform.localScale = Vector3.Lerp(baseScale, pulseScale, t);

            Color color = shatterSpriteRenderer.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            shatterSpriteRenderer.color = color;

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pulseDuration);
            portraitTransform.localScale = Vector3.Lerp(pulseScale, baseScale, t);
            yield return null;
        }

        portraitTransform.localScale = baseScale;

        Color finalColor = shatterSpriteRenderer.color;
        finalColor.a = targetAlpha;
        shatterSpriteRenderer.color = finalColor;
    }
        public void PlayPlayerHitSound()
    {
        if (playerHitAudio != null)
        {
            playerHitAudio.Play();
        }
    }

    public void PlayPlayerMissSound()
    {
        if (playerMissAudio != null)
        {
            playerMissAudio.Play();
        }
    }

    public void PlayGunshotSound()
    {
        if (gunshothAudio != null)
        {
            gunshothAudio.Play();
        }
    }

    public void PlaySniperGunshotSound()
    {
        if (sniperGunshotAudio != null)
        {
            sniperGunshotAudio.Play();
        }
    }

    void Awake() {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!LocalizationManager.IsInitialized)
        {
            LocalizationManager.Initialize();
        }

        hungarianSide = Random.Range(0,2) == 0;

        ChooseSoldier();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            InitializeExitConfirmationUI();
        }
        else
        {
            ClearExitConfirmationReferences();
        }
    }

    private void ClearExitConfirmationReferences()
    {
        exitConfirmationPanel = null;
        exitConfirmButton = null;
        exitCancelButton = null;
        exitConfirmationTitleText = null;
        exitConfirmationMessageText = null;
        exitConfirmationVisible = false;
    }

    [SerializeField] private CanvasGroup creditsCanvasGroup;

    void Credits(){
        StartCoroutine(FullDeath());
    }

    void ChooseSoldier()
    {
        SaveSoldiersToFile();
        if (HungarianSoldiers.Where(soldier => !soldier.picked).ToArray().Length == 0 &&
           RussianSoldiers.Where(soldier => !soldier.picked).ToArray().Length == 0)
        {
            // Minden katona ki lett választva, visszaállítjuk az állapotukat
            foreach (var soldier in HungarianSoldiers)
            {
                soldier.picked = false;
            }
            foreach (var soldier in RussianSoldiers)
            {
                soldier.picked = false;
            }
            Credits();
            return;
            
        }

        if(hungarianSide){
            if(HungarianSoldiers.Where(soldier => !soldier.picked).ToArray().Length == 0){
                hungarianSide = !hungarianSide;
            } 
        } else {
            if(RussianSoldiers.Where(soldier => !soldier.picked).ToArray().Length == 0){
                hungarianSide = !hungarianSide;
            } 
        }

        if (hungarianSide)
        {
            // Csak a nem kiválasztott magyar katonák szűrése
            var availableHungarianSoldiers = HungarianSoldiers.Where(soldier => !soldier.picked).ToArray();
            if (availableHungarianSoldiers.Length > 0)
            {
                currentSoldier = availableHungarianSoldiers[Random.Range(0, availableHungarianSoldiers.Length)];
                currentSoldier.picked = true; // Jelöld meg, hogy ki lett választva

                string language = LocalizationManager.CurrentLanguage;
                if (language == "Hungarian")
                {
                    subtitles.SetSubtitles(currentSoldier.hungarianEntries); // Magyar feliratok
                }
                else
                {
                    subtitles.SetSubtitles(currentSoldier.englishEntries); // Alapértelmezett (angol)
                }
            }
        }
        else
        {
            // Csak a nem kiválasztott orosz katonák szűrése
            var availableRussianSoldiers = RussianSoldiers.Where(soldier => !soldier.picked).ToArray();
            if (availableRussianSoldiers.Length > 0)
            {
                currentSoldier = availableRussianSoldiers[Random.Range(0, availableRussianSoldiers.Length)];
                currentSoldier.picked = true; // Jelöld meg, hogy ki lett választva

                string language = LocalizationManager.CurrentLanguage;
                if (language == "Hungarian")
                {
                    subtitles.SetSubtitles(currentSoldier.hungarianEntries); // Magyar feliratok
                }
                else
                {
                    subtitles.SetSubtitles(currentSoldier.englishEntries); // Alapértelmezett (angol)
                }
            }
        }


        Narration();
    }

    void Start() {
        if (cam == null) cam = Camera.main;

        //TODO: maybe only if starting in hiding state?

        
        isHiding = true;
        hideActive = false;
        if (crouchAudio != null){
                crouchAudio.Play();
        }
        camTargetY = camStartY - 10f; 
        //Cursor.lockState = CursorLockMode.Locked;
        license.transform.position = new Vector3(-7.5f, -14f, license.transform.position.z);
        camMoving = true;
        camLerpT = 0f;
        canShoot = false;

        ///////////

        LoadSoldiersFromFile();
        PlayAmbientSound();

        startPos = transform.position;
        Cursor.lockState = CursorLockMode.Confined; 
        Cursor.visible = false; 
        previousIsHiding = isHiding;
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("EnemySoldier");
        foreach (GameObject enemy in enemies)
        {
            enemySoldiers.Add(enemy.transform);
        }

        GameObject[] enemyM = GameObject.FindGameObjectsWithTag("EnemyMover");
        foreach (GameObject enemy in enemyM)
        {
            CursorMovementEnemy mover = enemy.GetComponent<CursorMovementEnemy>();
            if (mover != null)
            {
                enemyMovers.Add(mover);
            }
        }

        PositionLicense();

        if (fogRenderer == null)
        {
            fogRenderer = FindSceneGameObject("Fog")?.GetComponent<SpriteRenderer>();
        }

        if (fogRenderer != null)
        {
            fogBaseSpeed = fogRenderer.material.GetVector("Speed");
            fogBaseSpeed2 = fogRenderer.material.GetVector("Speed_2");
        }

        StartCoroutine(SceneStart());

        InitializeExitConfirmationUI();
        licenseInitialized = true;
    }

    IEnumerator SceneStart()
    {
        float duration = 2f;
        float elapsed = 0f;

        float ambientTarget  = ambient  != null ? ambient.volume  : 0f;
        float windTarget     = wind     != null ? wind.volume     : 0f;
        float distanceTarget = distance != null ? distance.volume : 0f;

        if (ambient  != null) ambient.volume  = 0f;
        if (wind     != null) wind.volume     = 0f;
        if (distance != null) distance.volume = 0f;

        CanvasGroup canvasGroup = canvas_opening != null
            ? canvas_opening.GetComponent<CanvasGroup>()
            : null;

        if (openingCaptionText == null)
        {
            openingCaptionText = FindSceneGameObject("Programming")?.GetComponent<TextMeshProUGUI>();
        }

        if (openingCaptionText != null)
        {
            openingCaptionText.text = LocalizationManager.GetText("GameScene.Opening.Caption", openingCaptionText.text);
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(3f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            if (ambient  != null) ambient.volume  = Mathf.Lerp(0f, ambientTarget,  t);
            if (wind     != null) wind.volume     = Mathf.Lerp(0f, windTarget,     t);
            if (distance != null) distance.volume = Mathf.Lerp(0f, distanceTarget, t);

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvas_opening.SetActive(false);
        }

        

        if (ambient  != null) ambient.volume  = ambientTarget;
        if (wind     != null) wind.volume     = windTarget;
        if (distance != null) distance.volume = distanceTarget;
    }

    public void Narration()
    {
        string language = LocalizationManager.CurrentLanguage;
        AudioClip narrationClip = null;

        if (language == "Hungarian")
        {
            narrationClip = currentSoldier.hungarianAudio; // Magyar nyelvű audio
        }
        else
        {
            narrationClip = currentSoldier.englishAudio; // Alapértelmezett (angol)
        }

        narrationAudio = gameObject.AddComponent<AudioSource>();
        narrationAudio.clip = narrationClip;
        narrationAudio.volume = 1.0f;
        narrationAudio.playOnAwake = false;

        if (startNarrationCoroutine != null)
        {
            StopCoroutine(startNarrationCoroutine);
        }

        startNarrationCoroutine = StartCoroutine(StartNarrationAfterDelay());
    }

    IEnumerator StartNarrationAfterDelay()
    {
        float standingTime = 0f;

        while (standingTime < 2f)
        {
            standingTime = isHiding ? 0f : standingTime + Time.deltaTime;
            yield return null;
        }

        if (!isHiding && narrationAudio != null)
        {
            narrationAudio.Play();
        }

        startNarrationCoroutine = null;
    }


    void Update() {

        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (exitConfirmationVisible)
            {
                HideExitConfirmation();
            }
            else
            {
                ShowExitConfirmation();
            }
        }

        if(isPlayerDead) {
            //license.SetActive(false); 
        }else{
            license.SetActive(true); 
        }

        // A guggolás / felállás logikája külön metódusba lett áthelyezve, ezért itt csak a hívás marad.
        if (Input.GetKeyDown(KeyCode.Space) && hideActive && !exitConfirmationVisible) {
            ToggleHideState();
        }

        UpdateRadioNoiseVolume();

        if (camMoving) {
            camLerpT += Time.deltaTime * camLerpSpeed;
            
            float newCamY = Mathf.Lerp(cam.transform.position.y, camTargetY, camLerpT);
            Vector3 camPos = cam.transform.position;
            camPos.y = newCamY;
            cam.transform.position = camPos;
            
            if (camLerpT >= 1f) {
                camMoving = false;
                hideActive = true;  
                if(!isHiding){
                    canShoot = true;
                }
            }
        }

        Vector3 mousePos = Input.mousePosition;
        float rawMouseX = (mousePos.x / Screen.width - 0.5f) * 2f;
        float rawMouseY = (mousePos.y / Screen.height - 0.5f) * 2f;
        float normalizedX = rawMouseX * parallaxFactorX;
        float normalizedY = rawMouseY * parallaxFactorY;

        Vector3 targetPos = startPos + new Vector3(normalizedX, normalizedY, 0);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);

        if (backgroundSprite != null) {
            backgroundSprite.transform.position = new Vector3(
                -normalizedX,  
                -normalizedY - maxOffsetY, 
                backgroundSprite.transform.position.z
            );
        }

        if (middlegroundSprite != null) {
            middlegroundSprite.transform.position = new Vector3(
                -normalizedX / 32f, 
                -normalizedY / 32f - 10f, 
                middlegroundSprite.transform.position.z
            );
        }

        foreach (CursorMovementEnemy mover in enemyMovers) {
            if (mover == null) continue;
            mover.SetNormalized(rawMouseX, rawMouseY);
        }

        if (!sideVisualsApplied || appliedHungarianSide != hungarianSide)
        {
            appliedHungarianSide = hungarianSide;
            sideVisualsApplied = true;

            if (hungarianSide) {
                background.sprite = hungarianBackground;
                flagHun.SetActive(true);
                flagRus.SetActive(false);
                if (ruinsCoverRenderer != null) {
                    ruinsCoverRenderer.sprite = hungarianRuinsSprite;
                    ruinsCoverRenderer.transform.localScale = new Vector3(hungarianRuinsScale, hungarianRuinsScale, 1f);
                }
                //middleground.sprite = hungarianMiddleground;
            } else {
                background.sprite = russianBackground;
                flagHun.SetActive(false);
                flagRus.SetActive(true);
                if (ruinsCoverRenderer != null) {
                    ruinsCoverRenderer.sprite = sovietRuinsSprite;
                    ruinsCoverRenderer.transform.localScale = new Vector3(sovietRuinsScale, sovietRuinsScale, 1f);
                }
                //middleground.sprite = russianMiddleground;
            }

            if (fogRenderer != null)
            {
                float fogDirection = hungarianSide ? -1f : 1f;
                fogRenderer.material.SetVector("Speed", fogBaseSpeed * fogDirection);
                fogRenderer.material.SetVector("Speed_2", fogBaseSpeed2 * fogDirection);
            }
        }

        PositionLicense();

        if(narrationAudio == null || narrationAudio.clip == null)
        {
            return;
        }
        if (!narrationAudio.isPlaying && narrationAudio.time >= narrationAudio.clip.length)
        {
            currentSoldier.isOpened = true; // Mark the soldier as opened when narration ends
            StartCoroutine(ChangeSoldier()); // Automatically choose the next soldier after narration ends
        }

        bool hidingJustActivated = isHiding && !previousIsHiding;
        previousIsHiding = isHiding;

        if (isHiding && narrationAudio.isPlaying)
        {
            if (narrationPauseTime == 0)
            {
                narrationPauseTime = narrationAudio.time;
            }

            if (hidingJustActivated)
            {
                PlayNarrationPauseWarningAudio();
            }

            if (stopNarrationCoroutine == null)
            {
                stopNarrationCoroutine = StartCoroutine(StopNarrationAfterDelay());
            }
        }
        else if (!isHiding)
        {
            if (stopNarrationCoroutine != null)
            {
                StopCoroutine(stopNarrationCoroutine);
                stopNarrationCoroutine = null;
            }

            StopNarrationPauseWarningAudio();

            if (!narrationAudio.isPlaying && narrationPauseTime > 0 && resumeNarrationCoroutine == null)
            {
                resumeNarrationCoroutine = StartCoroutine(ResumeNarrationAfterDelay());
            }
        }
    }

    private void ShowExitConfirmation()
    {
        if (!isHiding && hideActive)
        {
            ToggleHideState();
        }

        exitConfirmationVisible = true;
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);
        }
    }

    private void HideExitConfirmation()
    {
        exitConfirmationVisible = false;
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    private void ToggleHideState()
    {
        if (!hideActive)
        {
            return;
        }

        if (isHiding)
        {
            isHiding = false;
            hideActive = false;
            if (standUpAudio != null)
            {
                standUpAudio.Play();
            }
            camTargetY = camStartY;
            license.transform.position = new Vector3(-7.5f, -4f, license.transform.position.z);
        }
        else
        {
            isHiding = true;
            hideActive = false;
            PlayNarrationPauseWarningAudio();
            if (crouchAudio != null)
            {
                crouchAudio.Play();
            }
            camTargetY = camStartY - 10f;
            license.transform.position = new Vector3(-7.5f, -14f, license.transform.position.z);
        }

        camMoving = true;
        camLerpT = 0f;
        canShoot = false;
    }

    private void ConfirmExitToMainMenu()
    {
        exitConfirmationVisible = false;
        StopAllSceneAudio();
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("Menu");
    }

    private void InitializeExitConfirmationUI()
    {
        if (exitConfirmationPanel == null)
        {
            exitConfirmationPanel = FindSceneGameObject("ExitConfirmationPanel");
        }

        if (exitConfirmButton == null)
        {
            exitConfirmButton = FindSceneGameObject("ExitYesButton")?.GetComponent<Button>();
        }

        if (exitCancelButton == null)
        {
            exitCancelButton = FindSceneGameObject("ExitNoButton")?.GetComponent<Button>();
        }

        if (exitConfirmationTitleText == null)
        {
            exitConfirmationTitleText = FindSceneGameObject("ExitConfirmationTitle")?.GetComponent<TextMeshProUGUI>();
        }

        if (exitConfirmationMessageText == null)
        {
            exitConfirmationMessageText = FindSceneGameObject("ExitConfirmationMessage")?.GetComponent<TextMeshProUGUI>();
        }

        if (exitYesButtonText == null)
        {
            exitYesButtonText = FindSceneGameObject("ExitYesButtonText")?.GetComponent<TextMeshProUGUI>();
        }

        if (exitNoButtonText == null)
        {
            exitNoButtonText = FindSceneGameObject("ExitNoButtonText")?.GetComponent<TextMeshProUGUI>();
        }

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }

        if (exitConfirmButton != null)
        {
            exitConfirmButton.onClick.RemoveAllListeners();
            exitConfirmButton.onClick.AddListener(ConfirmExitToMainMenu);
        }

        if (exitCancelButton != null)
        {
            exitCancelButton.onClick.RemoveAllListeners();
            exitCancelButton.onClick.AddListener(HideExitConfirmation);
        }

        UpdateExitConfirmationText();
    }

    private void UpdateExitConfirmationText()
    {
        if (exitConfirmationTitleText != null)
        {
            exitConfirmationTitleText.text = exitConfirmationTitle;
        }

        if (exitConfirmationMessageText != null)
        {
            exitConfirmationMessageText.text = LocalizationManager.GetText("GameScene.ExitConfirmation.Message", exitConfirmationMessage);
        }

        if (exitYesButtonText != null)
        {
            exitYesButtonText.text = LocalizationManager.GetText("GameScene.ExitConfirmation.YesButton", exitYesButtonText.text);
        }

        if (exitNoButtonText != null)
        {
            exitNoButtonText.text = LocalizationManager.GetText("GameScene.ExitConfirmation.NoButton", exitNoButtonText.text);
        }
    }

    private GameObject FindSceneGameObject(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindChildRecursive(root.transform, name);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            var result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void StopAllSceneAudio()
    {
        foreach (var source in FindObjectsOfType<AudioSource>())
        {
            source.Stop();
        }

        if (narrationAudio != null)
        {
            narrationAudio.Stop();
        }
    }

    IEnumerator StopNarrationAfterDelay()
    {
        if (narrationPauseDelay > 0f)
        {
            yield return new WaitForSeconds(narrationPauseDelay);
        }

        if (isHiding)
        {
            narrationAudio.Pause(); // Pause instead of stopping to retain the current playback position
        }

        stopNarrationCoroutine = null;
    }

    IEnumerator ResumeNarrationAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (!isHiding)
        {
            //narrationAudio.time = Mathf.Clamp(narrationPauseTime, 0, narrationAudio.clip.length); // Ensure the seek position is valid
            narrationAudio.UnPause();
            narrationPauseTime = 0;
        }

        resumeNarrationCoroutine = null;
    }

    void UpdateRadioNoiseVolume()
    {
        if (radioNoiseAudio == null)
        {
            return;
        }

        radioNoiseTargetVolume = isHiding ? radioNoiseHidingVolume : radioNoiseNormalVolume;
        float fadeDuration = isHiding ? radioNoiseFadeInDuration : radioNoiseFadeOutDuration;

        if (fadeDuration <= 0f)
        {
            radioNoiseAudio.volume = radioNoiseTargetVolume;
            return;
        }

        float volumeStep = Mathf.Abs(radioNoiseHidingVolume - radioNoiseNormalVolume) * (Time.deltaTime / fadeDuration);
        radioNoiseAudio.volume = Mathf.MoveTowards(radioNoiseAudio.volume, radioNoiseTargetVolume, volumeStep);
    }

    void PlayNarrationPauseWarningAudio()
    {
        if (narrationPauseWarningAudio == null)
        {
            return;
        }

        if (narrationPauseWarningAudio.clip == null)
        {
            return;
        }

        if (!narrationPauseWarningAudio.isPlaying)
        {
            narrationPauseWarningAudio.Play();
        }
    }

    void StopNarrationPauseWarningAudio()
    {
        if (narrationPauseWarningAudio != null && narrationPauseWarningAudio.isPlaying)
        {
            narrationPauseWarningAudio.Stop();
        }
    }

    public List<Transform> GetEnemies(){
        return enemySoldiers;
    }

    public bool GetSide(){
        return hungarianSide;
    }

    public bool CanShoot(){
        return canShoot;
    }

    public bool IsHiding() {
        return isHiding;
    }
    public float GetSoldierHitChance()
    {
        int listenedCount = CountListenedNarrations();

        return listenedCount switch
        {
            0 => 0.3f,
            1 => 0.4f,
            2 => 0.5f,
            3 => 0.6f,
            4 => 0.7f,
            5 => 0.8f,
            6 => 0.9f,
            _ => 1f, 
        };
    }

    int CountListenedNarrations()
    {
        int hungarianCount = HungarianSoldiers?.Count(soldier => soldier.isOpened) ?? 0;
        int russianCount = RussianSoldiers?.Count(soldier => soldier.isOpened) ?? 0;
        return hungarianCount + russianCount;
    }

    public void PlayerDeath()
    {
        if (isPlayerDead) return;
        isPlayerDead = true;

        if (narrationAudio != null)
        {
            narrationAudio.Stop();
        }

        if (startNarrationCoroutine != null)
        {
            StopCoroutine(startNarrationCoroutine);
            startNarrationCoroutine = null;
        }

        if (stopNarrationCoroutine != null)
        {
            StopCoroutine(stopNarrationCoroutine);
            stopNarrationCoroutine = null;
        }

        if (resumeNarrationCoroutine != null)
        {
            StopCoroutine(resumeNarrationCoroutine);
            resumeNarrationCoroutine = null;
        }

        if (lifeRegenCoroutine != null)
        {
            StopCoroutine(lifeRegenCoroutine);
            lifeRegenCoroutine = null;
        }

        StopNarrationPauseWarningAudio();

        if (playerDeathAudio != null)
        {
            playerDeathAudio.Play();
        }

        StartCoroutine(FadeInDeath());
    }

    public int GetPlayerLives()
    {
        return playerLives;
    }


    [SerializeField] private Canvas creditsCanvas;

    [SerializeField] private EnemyManager em;
    IEnumerator FullDeath()
    {
        Cursor.lockState = CursorLockMode.None;

        RectTransform deathImageTransform = death.transform.GetChild(0) as RectTransform;
        Image deathSprite = deathImageTransform != null ? deathImageTransform.GetComponent<Image>() : null;
        deathSprite ??= death.GetComponent<Image>();
        if (deathSprite == null)
        {
            showOneLifeOnGameEnd = true;
            SceneManager.LoadScene("Menu");
            Destroy(em);
            yield break;
        }

        death.SetActive(true);

        Color color = deathSprite.color;
        float alpha = color.a;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime / 2f; // 2 másodperc alatt növeli az átlátszóságot
            color.a = Mathf.Clamp01(alpha);
            deathSprite.color = color;
            yield return null;
        }

        showOneLifeOnGameEnd = true;
        SceneManager.LoadScene("Menu");
        Destroy(em);
    }

    IEnumerator FadeInDeath()
    {
        Cursor.lockState = CursorLockMode.Locked;


        yield return new WaitForSeconds(0.5f); // Rövid késleltetés a halás animáció előtt
        // Enable death gameobject
        // Fade in the death image, 0.5f, death images is the first child of the death gameobject
        // Slow zoom out, 2s, parallel to the fade in, zooming happens by scaling the image, it should start from x=5, y=5, z=1, and end at x=1, y=1, z=1
        RectTransform deathImageTransform = death.transform.GetChild(0) as RectTransform;
        Image deathSprite = deathImageTransform.GetComponent<Image>() ?? death.GetComponent<Image>();
        death.SetActive(true);
        Vector3 zoomOutStartScale = new Vector3(5f, 5f, 1f);
        Vector3 zoomOutEndScale = Vector3.one;
        float fadeInDuration = 1.5f;
        float zoomOutDuration = 1.5f;

        RectTransform startGroup = hungarianSide ? hungarianXGroup : sovietXGroup;
        RectTransform endGroup = hungarianSide ? sovietXGroup : hungarianXGroup;
        RectTransform startMarker = PickRandomChildMarker(startGroup);
        RectTransform endMarker = PickRandomChildMarker(endGroup);
        
        Vector2 startAnchor = startMarker != null ? (Vector2)deathImageTransform.InverseTransformPoint(startMarker.position) : Vector2.zero;
        Vector2 endAnchor = endMarker != null ? (Vector2)deathImageTransform.InverseTransformPoint(endMarker.position) : Vector2.zero;

        Vector2 startOffsetAtMaxZoom = -startAnchor * zoomOutStartScale.x;
        Vector2 endOffsetAtMaxZoom = -endAnchor * zoomOutStartScale.x;
        Vector2 neutralOffset = Vector2.zero;

        SetAllMarkersVisible(hungarianXGroup, false);
        SetAllMarkersVisible(sovietXGroup, false);
        if (startMarker != null)
        {
            startMarker.gameObject.SetActive(true);
        }

        deathImageTransform.localScale = zoomOutStartScale;
        deathImageTransform.anchoredPosition = startOffsetAtMaxZoom;
        if (deathSprite != null)
        {
            Color c = deathSprite.color;
            c.a = 0f;
            deathSprite.color = c;
        }
        float elapsed = 0f;
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float zoomT = Mathf.Clamp01(elapsed / zoomOutDuration);
            float scale = Mathf.Lerp(zoomOutStartScale.x, zoomOutEndScale.x, zoomT);
            deathImageTransform.localScale = new Vector3(scale, scale, 1f);
            deathImageTransform.anchoredPosition = Vector2.Lerp(startOffsetAtMaxZoom, neutralOffset, zoomT);

            if (deathSprite != null)
            {
                float alphaT = Mathf.Clamp01(elapsed / fadeInDuration);
                Color c = deathSprite.color;
                c.a = alphaT;
                deathSprite.color = c;
            }

            yield return null;
        }
        deathImageTransform.localScale = zoomOutEndScale;
        deathImageTransform.anchoredPosition = neutralOffset; 
        if (deathSprite != null)
        {
            Color c = deathSprite.color;
            c.a = 1f;
            deathSprite.color = c;
        }

        EnemyManager.Instance.ResetAllEnemies();

        hungarianSide = !hungarianSide;

        // Short hold distance
        // Fast zoom in 0.5s, it should start from x=1, y=1, z=1, and end at  x=5, y=5, z=1
        // Fade out the death image, parallel to the zoom in
        // Disable gameobject
        float holdDuration = 0.5f;
        yield return new WaitForSeconds(holdDuration);
        float zoomInDuration = 0.5f;
        elapsed = 0f;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInDuration);

            float scale = Mathf.Lerp(zoomOutEndScale.x, zoomOutStartScale.x, t);
            deathImageTransform.localScale = new Vector3(scale, scale, 1f);
            deathImageTransform.anchoredPosition = Vector2.Lerp(neutralOffset, endOffsetAtMaxZoom, t);

            if (deathSprite != null)
            {
                Color c = deathSprite.color;
                c.a = 1f - t;
                deathSprite.color = c;
            }

            yield return null;
        }
        deathImageTransform.localScale = zoomOutStartScale;
        if (deathSprite != null)
        {
            Color c = deathSprite.color;
            c.a = 0f;
            deathSprite.color = c;
        }
        death.SetActive(false);

        Cursor.lockState = CursorLockMode.Confined;
        ChooseSoldier();
        isPlayerDead = false;
        playerLives = maxPlayerLives;

        SetShatterPortraitAlpha(playerLives);

        yield return new WaitForSeconds(1f);
    }

    RectTransform PickRandomChildMarker(RectTransform group)
    {
        if (group == null || group.childCount == 0)
        {
            return null;
        }

        return group.GetChild(Random.Range(0, group.childCount)) as RectTransform;
    }

    void SetAllMarkersVisible(RectTransform group, bool visible)
    {
        if (group == null)
        {
            return;
        }

        for (int i = 0; i < group.childCount; i++)
        {
            group.GetChild(i).gameObject.SetActive(visible);
        }
    }

    void SetShatterPortraitAlpha(int lives)
    {
        if (shatterSpriteRenderer == null)
        {
            shatterSpriteRenderer = license.transform.GetChild(0).GetComponent<SpriteRenderer>();
        }

        var color = shatterSpriteRenderer.color;

        if(playerLives == maxPlayerLives)
        {
            color.a = 0f;
            shatterSpriteRenderer.color = color;
            Debug.Log($"Color alpha set to 0 for {currentSoldier.Name} with {playerLives} lives");
        }
        else
        {
            color.a = 1f / (System.Math.Max(playerLives, 0) + 1f);
            shatterSpriteRenderer.color = color;
            Debug.Log($"Color alpha set to {color.a} for {currentSoldier.Name} with {playerLives} lives");
        }
    }

    void BuildNarrationCompleteUI()
    {
        if (narrationCompleteUIBuilt)
        {
            return;
        }
        narrationCompleteUIBuilt = true;

        GameObject canvasGO = new GameObject("Canvas-NarrationComplete");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        narrationCompleteCanvasGroup = canvasGO.AddComponent<CanvasGroup>();
        narrationCompleteCanvasGroup.alpha = 0f;
        narrationCompleteCanvasGroup.interactable = false;
        narrationCompleteCanvasGroup.blocksRaycasts = false;

        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(canvasGO.transform, false);
        Image background = backgroundGO.AddComponent<Image>();
        background.color = Color.black;
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject portraitGO = new GameObject("Portrait");
        portraitGO.transform.SetParent(canvasGO.transform, false);
        narrationCompletePortraitImage = portraitGO.AddComponent<Image>();
        narrationCompletePortraitImage.preserveAspect = true;
        RectTransform portraitRect = narrationCompletePortraitImage.rectTransform;
        portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
        portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(-220f, 0f);
        portraitRect.sizeDelta = new Vector2(320f, 320f);

        GameObject textGO = new GameObject("NarrationCompleteText");
        textGO.transform.SetParent(canvasGO.transform, false);
        narrationCompleteText = textGO.AddComponent<TextMeshProUGUI>();

        if (openingCaptionText != null)
        {
            narrationCompleteText.font = openingCaptionText.font;
            narrationCompleteText.fontSharedMaterial = openingCaptionText.fontSharedMaterial;
        }

        narrationCompleteText.color = Color.white;
        narrationCompleteText.fontSize = 28f;
        narrationCompleteText.alignment = TextAlignmentOptions.MidlineLeft;
        narrationCompleteText.enableWordWrapping = true;

        RectTransform textRect = narrationCompleteText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(150f, 0f);
        textRect.sizeDelta = new Vector2(560f, 200f);

        canvasGO.SetActive(false);
    }

    IEnumerator ChangeSoldier()
    {
        Cursor.lockState = CursorLockMode.Locked;

        BuildNarrationCompleteUI();

        if (narrationCompletePortraitImage != null)
        {
            narrationCompletePortraitImage.sprite = currentSoldier?.Image;
        }

        if (narrationCompleteText != null)
        {
            string messageTemplate = LocalizationManager.GetText(
                "GameScene.NarrationComplete.Message",
                "You have listened to {0}'s story.");
            narrationCompleteText.text = string.Format(messageTemplate, currentSoldier?.Name);
        }

        narrationCompleteCanvasGroup.gameObject.SetActive(true);
        narrationCompleteCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < narrationCompleteFadeInDuration)
        {
            elapsed += Time.deltaTime;
            narrationCompleteCanvasGroup.alpha = Mathf.Clamp01(elapsed / narrationCompleteFadeInDuration);
            yield return null;
        }
        narrationCompleteCanvasGroup.alpha = 1f;

        EnemyManager.Instance.ResetAllEnemies();

        //hungarianSide = !hungarianSide; erre itt nincs szükség, mert a katonák váltogatása után is ugyanazon az oldalon maradunk

        yield return new WaitForSeconds(narrationCompleteHoldDuration);

        narrationCompleteCanvasGroup.alpha = 0f;
        narrationCompleteCanvasGroup.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Confined;
        ChooseSoldier();
        isPlayerDead = false;
        playerLives = 3;
        SetShatterPortraitAlpha(playerLives);
    }

    

    void PositionLicense()
{
    if (license == null || cam == null || !licenseInitialized) return;

    if (licenseSpriteRenderer == null)
    {
        licenseSpriteRenderer = license.GetComponent<SpriteRenderer>();
    }

    Vector3 licenseOffset = licenseViewportOffset;
    licenseOffset.y = 0.1f;

    Vector3 worldPos = cam.ViewportToWorldPoint(licenseOffset);
    license.transform.position = worldPos;

    licenseSpriteRenderer.sprite = currentSoldier?.Image;
    licenseSpriteRenderer.sortingOrder = 100;
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
            NarrationLanguage = LocalizationManager.CurrentLanguage, // Nyelv mentése
            SubtitlesEnabled = PlayerPrefs.GetInt("SubtitlesEnabled", 1) == 1,          // Felirat állapot mentése
            SubtitlesLanguage = PlayerPrefs.GetString("SubtitlesLanguage", "English")            // Felirat nyelv mentése
        };

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
    }

    public void PlayAmbientSound()
    {
        if (ambient != null)
        {
            ambient.loop = true;
            ambient.Play();
            wind.loop = true;
            wind.Play();
            distance.loop = true;
            distance.Play();
        }

        if (radioNoiseAudio != null)
        {
            radioNoiseAudio.loop = true;
            radioNoiseAudio.volume = radioNoiseNormalVolume;
            radioNoiseAudio.Play();
        }
    }

}
