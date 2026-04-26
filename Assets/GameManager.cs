using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour 
{
    [Header("Menu Navigation")]
    public bool showCreditsOnGameEnd = false;
    
    [SerializeField] private Subtitles subtitles;

    [SerializeField] private AudioSource ambient;
    [SerializeField] private AudioSource wind;
    [SerializeField] private AudioSource distance;
    [SerializeField] private AudioSource radioNoiseAudio;
    [SerializeField] private AudioSource narrationPauseWarningAudio;
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

    [SerializeField] private AudioSource playerHitAudio;
    [SerializeField] private AudioSource playerMissAudio;
    [SerializeField] private AudioSource playerDeathAudio;
    [SerializeField] private AudioSource gunshothAudio;


    private Vector3 startPos;
    public GameObject backgroundSprite;  
    public GameObject middlegroundSprite;
    public GameObject canvas_opening;    
    private List<Transform> enemySoldiers = new List<Transform>();
    private List<Transform> enemyMovers = new List<Transform>();

    
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

    private bool isLock = false;
    private int maxPlayerLives = 3;

    public static GameManager Instance { get; private set; }
    
    [SerializeField] private SoldierData[] HungarianSoldiers;
    [SerializeField] private SoldierData[] RussianSoldiers;

    SoldierData currentSoldier;

    private AudioSource narrationAudio;
    private float narrationPauseTime;
    private Coroutine stopNarrationCoroutine;
    private Coroutine resumeNarrationCoroutine;
    [SerializeField] private float narrationPauseDelay = 3f;
    [SerializeField] private float narrationPauseWarningLeadTime = 2f;

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
        }
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

    void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        hungarianSide = Random.Range(0,2) == 0;

        ChooseSoldier();
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

                string language = PlayerPrefs.GetString("NarrationLanguage", "English");
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

                string language = PlayerPrefs.GetString("NarrationLanguage", "English");
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
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("EnemySoldier");
        foreach (GameObject enemy in enemies)
        {
            enemySoldiers.Add(enemy.transform);
        }

        GameObject[] enemyM = GameObject.FindGameObjectsWithTag("EnemyMover");
        foreach (GameObject enemy in enemyM)
        {
            enemyMovers.Add(enemy.transform);
        }

        PositionLicense();

        StartCoroutine(SceneStart()); 

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
        string language = PlayerPrefs.GetString("NarrationLanguage", "English");
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

        StartCoroutine(StartNarrationAfterDelay());
    }

    IEnumerator StartNarrationAfterDelay()
    {
        yield return new WaitForSeconds(2f); // Resume from the last paused time
        narrationAudio.Play();
    }


    void Update() {

        if(Input.GetKeyDown(KeyCode.Escape)){
            PlayerDeath();
        }

        Debug.Log("Player élete: " + isPlayerDead);
        if(isPlayerDead) {
            //license.SetActive(false); 
        }else{
            license.SetActive(true); 
        }

        if (Input.GetKeyDown(KeyCode.Space) && hideActive) {
            if(isHiding){
                isHiding = false;
                hideActive = false;
                if (standUpAudio != null){
                        standUpAudio.Play();
                    }
                camTargetY = camStartY;  
                //Cursor.lockState = CursorLockMode.Confined;
                license.transform.position = new Vector3(-7.5f, -4f, license.transform.position.z);
            } else {
                isHiding = true;
                hideActive = false;
                if (crouchAudio != null){
                        crouchAudio.Play();
                }
                camTargetY = camStartY - 10f; 
                //Cursor.lockState = CursorLockMode.Locked;
                license.transform.position = new Vector3(-7.5f, -14f, license.transform.position.z);
            }
            camMoving = true;
            camLerpT = 0f;
            canShoot = false;
        }

        if (license != null && cam != null) {
            PositionLicense();
        }


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

        float normalizedX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f * parallaxFactorX;
        float normalizedY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f * parallaxFactorY;

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

        foreach (Transform enemy in enemyMovers) {
            if (enemy == null) continue;  
            enemy.GetComponent<CursorMovementEnemy>().SetNormalized((Input.mousePosition.x / Screen.width - 0.5f) * 2f, (Input.mousePosition.y / Screen.height - 0.5f) * 2f);
        }

        if(hungarianSide){
            background.sprite = hungarianBackground;
            flagHun.active = true;
            flagRus.active = false;
            //middleground.sprite = hungarianMiddleground;
        }else{
            background.sprite = russianBackground;
            flagHun.active = false;
            flagRus.active = true;
            //middleground.sprite = russianMiddleground;
        }



        PositionLicense();

        if(narrationAudio == null || narrationAudio.clip == null) return;
        if (!narrationAudio.isPlaying && narrationAudio.time >= narrationAudio.clip.length)
        {
            currentSoldier.isOpened = true; // Mark the soldier as opened when narration ends
            StartCoroutine(ChangeSoldier()); // Automatically choose the next soldier after narration ends
        }

        if (isHiding && narrationAudio.isPlaying)
        {
            if (narrationPauseTime == 0)
            {
                narrationPauseTime = narrationAudio.time;
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

    IEnumerator StopNarrationAfterDelay()
    {
        float warningDelay = Mathf.Max(0f, narrationPauseDelay - narrationPauseWarningLeadTime);

        if (warningDelay > 0f)
        {
            yield return new WaitForSeconds(warningDelay);
        }

        if (isHiding && narrationAudio != null && narrationAudio.isPlaying)
        {
            PlayNarrationPauseWarningAudio();
        }

        float remainingDelay = Mathf.Min(narrationPauseDelay, narrationPauseWarningLeadTime);
        if (remainingDelay > 0f)
        {
            yield return new WaitForSeconds(remainingDelay);
        }

        if (isHiding)
        {
            narrationAudio.Pause(); // Pause instead of stopping to retain the current playback position
            StopNarrationPauseWarningAudio();
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

    void PlayNarrationPauseWarningAudio()
    {
        if (narrationPauseWarningAudio != null && !narrationPauseWarningAudio.isPlaying)
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

    public void PlayerDeath()
    {
        if (isPlayerDead) return;
        isPlayerDead = true;

        if (narrationAudio != null)
        {
            narrationAudio.Stop();
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
        SpriteRenderer spriteRenderer = death.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            yield break;
        }

        Color color = spriteRenderer.color;
        float alpha = color.a;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime / 2f; // 2 másodperc alatt növeli az átlátszóságot
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
            yield return null;
        }

        showCreditsOnGameEnd = true;
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
        Transform deathImageTransform = death.transform.GetChild(0);
        Image deathSprite = deathImageTransform.GetComponent<Image>() ?? death.GetComponent<Image>();
        death.SetActive(true);
        Vector3 zoomOutStartScale = new Vector3(5f, 5f, 1f);
        Vector3 zoomOutEndScale = Vector3.one;
        float fadeInDuration = 1.5f;
        float zoomOutDuration = 1.5f;
        deathImageTransform.localScale = zoomOutStartScale;
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
            deathImageTransform.localScale = Vector3.Lerp(zoomOutStartScale, zoomOutEndScale, zoomT);

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

            deathImageTransform.localScale = Vector3.Lerp(zoomOutEndScale, zoomOutStartScale, t);

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

    void SetShatterPortraitAlpha(int lives)
    {
        var shatter = license.transform.GetChild(0);

        if (shatter == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = shatter.GetComponent<SpriteRenderer>();
        var color = spriteRenderer.color;

        if(playerLives == maxPlayerLives)
        {
            color.a = 0f;
            spriteRenderer.color = color;
            Debug.Log($"Color alpha set to 0 for {currentSoldier.Name} with {playerLives} lives");
        }
        else
        {
            color.a = 1f / (System.Math.Max(playerLives, 0) + 1f);
            spriteRenderer.color = color;
            Debug.Log($"Color alpha set to {color.a} for {currentSoldier.Name} with {playerLives} lives");
        }
    }

    IEnumerator ChangeSoldier()
    {
        Cursor.lockState = CursorLockMode.Locked;
        SpriteRenderer spriteRenderer = death.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            yield break;
        }

        Color color = spriteRenderer.color;
        float alpha = color.a;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime / 2f; // 2 másodperc alatt növeli az átlátszóságot
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        EnemyManager.Instance.ResetAllEnemies();

        //hungarianSide = !hungarianSide; erre itt nincs szükség, mert a katonák váltogatása után is ugyanazon az oldalon maradunk

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime / 2f; // 2 másodperc alatt csökkenti az átlátszóságot
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
            yield return null;
        }
        Cursor.lockState = CursorLockMode.Confined;
        ChooseSoldier();
        isPlayerDead = false;
        playerLives = 3;

        yield return new WaitForSeconds(1f);
    }

    

    void PositionLicense()
{
    if (license == null || cam == null || !licenseInitialized) return;
    
    Vector3 licenseOffset = licenseViewportOffset;
    
    if (isHiding) {
        licenseOffset.y = 0.1f;  // Hiding: 15% alulról (magasabb)
    } else {
        licenseOffset.y = 0.1f;  // Normál: 25% alulról (még magasabb)
    }
    
    Vector3 worldPos = cam.ViewportToWorldPoint(licenseOffset);
    license.transform.position = worldPos;
    
    license.GetComponent<SpriteRenderer>().sprite = currentSoldier?.Image;
    license.GetComponent<SpriteRenderer>().sortingOrder = 100;
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
            NarrationLanguage = PlayerPrefs.GetString("NarrationLanguage", "English"), // Nyelv mentése
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
            radioNoiseAudio.Play();
        }
    }

}
