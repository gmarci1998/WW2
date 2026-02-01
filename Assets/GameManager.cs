using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class GameManager : MonoBehaviour 
{
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

    void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        hungarianSide = Random.Range(0,2) == 0;

        ChooseSoldier();
    }

    void ChooseSoldier()
    {
        if (hungarianSide)
        {
            // Csak a nem kiválasztott magyar katonák szűrése
            var availableHungarianSoldiers = HungarianSoldiers.Where(soldier => !soldier.picked).ToArray();
            if (availableHungarianSoldiers.Length > 0)
            {
                currentSoldier = availableHungarianSoldiers[Random.Range(0, availableHungarianSoldiers.Length)];
                currentSoldier.picked = true; // Jelöld meg, hogy ki lett választva
            }
            else
            {
                Debug.LogWarning("Nincs több elérhető magyar katona!");
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
            else
            {
                Debug.LogWarning("Nincs több elérhető orosz katona!");
            }
        }

        Debug.Log("Kiválasztott katona: " + currentSoldier.Name);
    }

    void Start() {
        if (cam == null) cam = Camera.main;
        startPos = transform.position;
        Cursor.lockState = CursorLockMode.Confined;  
        
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

    void Update() {

        if(Input.GetKeyDown(KeyCode.Escape)){
            isLock = true;  
        }

        if (Input.GetKeyDown(KeyCode.Space) && hideActive) {
            if(isHiding){
                isHiding = false;
                hideActive = false;
                camTargetY = camStartY;  
                Cursor.lockState = CursorLockMode.Confined;
            } else {
                isHiding = true;
                hideActive = false;
                camTargetY = camStartY - 10f; 
                Cursor.lockState = CursorLockMode.Locked;
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
                normalizedX / 4f, 
                -4.2f, 
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
        Debug.Log("Player Died!");

        StartCoroutine(FadeInDeath());
    }

    IEnumerator FadeInDeath()
    {
        Cursor.lockState = CursorLockMode.Locked;
        SpriteRenderer spriteRenderer = death.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer not found on death GameObject!");
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
    }

    void PositionLicense()
    {
        if (license != null)
        {
            Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
            license.transform.position = new Vector3(bottomLeft.x + 0.5f, bottomLeft.y + 0.5f, license.transform.position.z);
        }
        else
        {
            Debug.LogError("A license GameObject nincs hozzárendelve!");
        }
    }
}
