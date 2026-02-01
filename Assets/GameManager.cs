using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour 
{
    [SerializeField] private AudioSource ambient;
    [SerializeField] private AudioSource wind;
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


    private Vector3 startPos;
    public GameObject backgroundSprite;  
    public GameObject middlegroundSprite;  
    private List<Transform> enemySoldiers = new List<Transform>();
    private List<Transform> enemyMovers = new List<Transform>();

    private bool isHiding = false;
    private bool hideActive = true;
    private float camStartY;           
    private float camTargetY;          
    private float camLerpT = 0f;      
    private bool camMoving = false;    
    private float camLerpSpeed = 2f;  
    private bool canShoot = true;

    private bool isLock = false;

    public static GameManager Instance { get; private set; }
    
    [SerializeField] private SoldierData[] HungarianSoldiers;
    [SerializeField] private SoldierData[] RussianSoldiers;

    SoldierData currentSoldier;

    private AudioSource narrationAudio;
    private float narrationPauseTime;

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
        if(HungarianSoldiers.Where(soldier => !soldier.picked).ToArray().Length == 0 &&
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
            SaveSoldiersToFile();
            Credits();
            return;
            
        }
        if (hungarianSide)
        {
            // Csak a nem kiválasztott magyar katonák szűrése
            var availableHungarianSoldiers = HungarianSoldiers.Where(soldier => !soldier.picked).ToArray();
            if (availableHungarianSoldiers.Length > 0)
            {
                currentSoldier = availableHungarianSoldiers[Random.Range(0, availableHungarianSoldiers.Length)];
                currentSoldier.picked = true; // Jelöld meg, hogy ki lett választva
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
            }
        }

        Narration();
    }

    void Start() {
        if (cam == null) cam = Camera.main;

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
    }

    public void Narration()
    {
        narrationAudio = gameObject.AddComponent<AudioSource>();
        narrationAudio.clip = currentSoldier.Audio;
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
            Credits();
        }

        if (Input.GetKeyDown(KeyCode.Space) && hideActive) {
            if(isHiding){
                isHiding = false;
                hideActive = false;
                camTargetY = camStartY;  
                Cursor.lockState = CursorLockMode.Confined;
                license.transform.position = new Vector3(-7.5f, -4f, license.transform.position.z);
            } else {
                isHiding = true;
                hideActive = false;
                camTargetY = camStartY - 10f; 
                Cursor.lockState = CursorLockMode.Locked;
                license.transform.position = new Vector3(-7.5f, -14f, license.transform.position.z);
            }
            camMoving = true;
            camLerpT = 0f;
            canShoot = false;
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

        if(narrationAudio == null || narrationAudio.clip == null) return;
        if (!narrationAudio.isPlaying && narrationAudio.time >= narrationAudio.clip.length)
        {
            currentSoldier.isOpened = true; // Mark the soldier as opened when narration ends
        }

        if (isHiding && narrationAudio.isPlaying)
        {
            if (narrationPauseTime == 0)
            {
                narrationPauseTime = narrationAudio.time;
            }

            StartCoroutine(StopNarrationAfterDelay());
        }
        else if (!isHiding && !narrationAudio.isPlaying && narrationPauseTime > 0)
        {
            StartCoroutine(ResumeNarrationAfterDelay());
        }
    }

    IEnumerator StopNarrationAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (isHiding)
        {
            narrationAudio.Pause(); // Pause instead of stopping to retain the current playback position
        }
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

        narrationAudio.Stop();

        StartCoroutine(FadeInDeath());
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

        SceneManager.LoadScene("Menu");
        Destroy(em);
        Destroy(this);
    }

    IEnumerator FadeInDeath()
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

        hungarianSide = !hungarianSide;

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime / 2f; // 2 másodperc alatt csökkenti az átlátszóságot
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
            yield return null;
        }
        Cursor.lockState = CursorLockMode.Confined;
        ChooseSoldier();
        
        yield return new WaitForSeconds(1f);
    }

    

    void PositionLicense()
    {
        if (license != null)
        {
            license.transform.position = new Vector3(-7.5f, -4f, license.transform.position.z);
            license.GetComponent<SpriteRenderer>().sprite = currentSoldier.Image;

        }
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

    SaveWrapper wrapper = new SaveWrapper { Soldiers = saveData };
    string json = JsonUtility.ToJson(wrapper, true);

    string filePath = Path.Combine(Application.persistentDataPath, "SoldiersData.json");
    File.WriteAllText(filePath, json);

    //Debug.Log("✅ SAVED: " + filePath);
    //Debug.Log("JSON: " + json);
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
    }

    public void PlayAmbientSound()
    {
        if (ambient != null)
        {
            ambient.loop = true;
            ambient.Play();
            wind.loop = true;
            wind.Play();
        }
        else
        {
            //Debug.LogWarning("Ambient AudioSource or AudioClip is not assigned.");
        }
    }

}
