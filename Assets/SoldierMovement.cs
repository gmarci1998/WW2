using UnityEngine;
using System.Collections;
using System;
using Random = UnityEngine.Random;  // Coroutine-hez

public class SoldierMovement : MonoBehaviour
{
    public enum EnemyType
    {
        Soldier,
        Sniper
    }
    [SerializeField] private float enemyParallaxX = 4f;
    [SerializeField] private float enemyParallaxY = 0.2f;
    [SerializeField] private float moveSpeed = 2f;
    private float targetHeight;
    private bool isMoving = false;
    private float waitForShooting = 4f;
    [SerializeField] private float duckTime = 1f;
    [Tooltip("Sniper lövésnél a lövés (hang/villanás) és a tényleges találat-ellenőrzés között eltelő idő - hosszabb reakcióidőt ad a játékosnak, mint a sima katonánál.")]
    [SerializeField] private float sniperHitDelay = 2f;
    private float firingTimer = 0f;
    private GameObject spark;

    [SerializeField] private float startingHeight = 0f;
    [SerializeField] private float maximumHeight = 3f; 
    private float? externalRiseAmount = null;

    [SerializeField] private float quickPeekHoldTime = 0.25f; 
    [SerializeField] private float mediumPeekHoldTime = 2f;   
    [SerializeField] private float longPeekFireDelay = 4f;   

    private int nextAction;
    private bool actionComplete = false;

    [SerializeField] private float timeBeforeFirstAction = 3f;
    [SerializeField] private EnemyType enemyType = EnemyType.Soldier;

    [Header("Sprite animáció (enemy_spritesheet.png, 1-10 sorrendben)")]
    [Tooltip("A csatolt referenciaképen látható 1-10 sorszámú frame-ek, ugyanabban a sorrendben.")]
    [SerializeField] private Sprite[] frames = new Sprite[10];
    [Tooltip("Fel-/lemenéskor és lövéskor használt frame-hossz.")]
    [SerializeField] private float frameDuration = 0.12f;
    [Tooltip("Álldogáláskor (idle loop) egy-egy frame ennyi véletlen idő között marad kint, másodpercben.")]
    [SerializeField] private float idleFrameDurationMin = 1f;
    [SerializeField] private float idleFrameDurationMax = 3f;
    [Tooltip("Ha az enemy lőni fog, ennyi másodperccel a lövés előtt vált a lövő pózra, és tart ki lövésig.")]
    [SerializeField] private float fireFrameLeadTime = 3f;
    [SerializeField] private float deathFrameDuration = 0.15f;
    [Tooltip("Milyen sebességgel süllyed vissza az enemy a fedezék mögé, miután meghalt.")]
    [SerializeField] private float deathSinkSpeed = 0.18f;

    private static readonly int[] RiseFrameSequence = { 0, 1 };
    private static readonly int[] IdleFrameSequence = { 1, 2, 1, 3, 1 };
    private static readonly int[] FireFrameSequence = { 9 };
    private static readonly int[] DeathFrameSequence = { 4, 5, 6, 7, 8 };
    private static readonly int[] DescendFrameSequence = { 1, 0 };

    private SpriteRenderer spriteRenderer;
    private Coroutine spriteAnimCoroutine;

    [Header("Sniper - csillanás (shine) figyelmeztetés")]
    [Tooltip("A csillanás sprite gyerek objektuma - csak a sniper prefabon kell bedrótozni, soldiernél üresen hagyható.")]
    [SerializeField] private GameObject shine;
    [SerializeField] private float shineWarningBeforeFirst = 3f; 
    [SerializeField] private float shineWarningBeforeSecond = 1f; 
    [SerializeField] private float shineAnimationDuration = 0.5f;
    [SerializeField] private float shineMinScale = 0.05f;
    [SerializeField] private float shineMaxScale = 1f;
    [SerializeField] private float shineRotationDegrees = 25f;

    public bool IsDead { get; set; } = false;
    public bool IsKillable { get; set; } = false;

    public Action OnDeathDelegate { get; set; }

    void Start()
    {
        startingHeight = transform.localPosition.y;

        float riseAmount = externalRiseAmount ?? maximumHeight;
        maximumHeight = startingHeight + riseAmount;

        spriteRenderer = GetComponent<SpriteRenderer>();
        SetFrame(DescendFrameSequence[^1]);

        StartCoroutine(DecideAndActDelayed());
        spark = transform.GetChild(0).gameObject;
    }

    void SetFrame(int frameIndex)
    {
        if (spriteRenderer == null || frames == null || frameIndex < 0 || frameIndex >= frames.Length)
        {
            return;
        }

        Sprite frame = frames[frameIndex];
        if (frame != null)
        {
            spriteRenderer.sprite = frame;
        }
    }

    void PlaySpriteAnimation(int[] frameSequence, float duration, bool loop)
    {
        PlaySpriteAnimation(frameSequence, duration, duration, loop);
    }

    void PlaySpriteAnimation(int[] frameSequence, float minDuration, float maxDuration, bool loop)
    {
        if (spriteAnimCoroutine != null)
        {
            StopCoroutine(spriteAnimCoroutine);
        }

        spriteAnimCoroutine = StartCoroutine(SpriteAnimationRoutine(frameSequence, minDuration, maxDuration, loop));
    }

    IEnumerator SpriteAnimationRoutine(int[] frameSequence, float minDuration, float maxDuration, bool loop)
    {
        do
        {
            foreach (int frameIndex in frameSequence)
            {
                SetFrame(frameIndex);
                yield return new WaitForSeconds(Random.Range(minDuration, maxDuration));
            }
        } while (loop);
    }

    public void SetPeekRiseAmount(float riseAmount)
    {
        externalRiseAmount = riseAmount;
    }

    void Update()
    {
    }

    IEnumerator DecideAndActDelayed()
    {
        yield return new WaitForSeconds(timeBeforeFirstAction);

        DecideAndAct();
    }

    void DecideAndAct()
    {
        nextAction = DecideAction();
        targetHeight = maximumHeight; 

        switch (nextAction)
        {
            case 0:
                StartCoroutine(StayCrouchedAndDecideAgain());
                break;
            case 1:
                StartCoroutine(QuickPeekAndReturn());
                break;
            case 2:
                StartCoroutine(PeekAndReturn());
                break;
            case 3:
                StartCoroutine(FullyEmergeAndReturn());
                break;
        }
    }

    int DecideAction()
    {
        if (GameManager.Instance.IsHiding())
        {
            return Random.Range(0, 3); 
        }

        return Random.Range(0, 4); 
    }

    IEnumerator StayCrouchedAndDecideAgain()
    {
        IsKillable = false;
        yield return new WaitForSeconds(Random.Range(2f, 5f));
        DecideAndAct();
    }

    IEnumerator RiseTo(float height)
    {
        yield return MoveTo(height, moveSpeed);
    }

    IEnumerator MoveTo(float height, float speed)
    {
        while (Mathf.Abs(transform.localPosition.y - height) > 0.01f)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, new Vector3(transform.localPosition.x, height, transform.localPosition.z), speed * deltaTime);
            yield return new WaitForEndOfFrame();
        }

        transform.localPosition = new Vector3(transform.localPosition.x, height, transform.localPosition.z);
    }

    IEnumerator QuickPeekAndReturn()
    {
        isMoving = true;
        PlaySpriteAnimation(RiseFrameSequence, frameDuration, false);

        yield return RiseTo(targetHeight);

        IsKillable = true;
        PlaySpriteAnimation(IdleFrameSequence, idleFrameDurationMin, idleFrameDurationMax, true);
        yield return new WaitForSeconds(quickPeekHoldTime);
        IsKillable = false;

        PlaySpriteAnimation(DescendFrameSequence, frameDuration, false);
        yield return RiseTo(startingHeight);

        isMoving = false;
        yield return new WaitForSeconds(1f);
        DecideAndAct();
    }

    IEnumerator PeekAndReturn()
    {
        isMoving = true;
        PlaySpriteAnimation(RiseFrameSequence, frameDuration, false);

        yield return RiseTo(targetHeight);

        IsKillable = true;
        PlaySpriteAnimation(IdleFrameSequence, idleFrameDurationMin, idleFrameDurationMax, true);
        yield return new WaitForSeconds(mediumPeekHoldTime);
        IsKillable = false;

        PlaySpriteAnimation(DescendFrameSequence, frameDuration, false);
        yield return RiseTo(startingHeight);

        isMoving = false;
        yield return new WaitForSeconds(1f); // Visszatérés után vár
        DecideAndAct();
    }

    IEnumerator FullyEmergeAndReturn()
    {
        isMoving = true;
        PlaySpriteAnimation(RiseFrameSequence, frameDuration, false);

        yield return RiseTo(targetHeight);

        IsKillable = true;
        PlaySpriteAnimation(IdleFrameSequence, idleFrameDurationMin, idleFrameDurationMax, true);
        firingTimer = longPeekFireDelay;

        StartCoroutine(FireAtPlayer());

        yield return new WaitForSeconds(firingTimer + GetPostShotDelay());
        IsKillable = false;

        PlaySpriteAnimation(DescendFrameSequence, frameDuration, false);
        yield return RiseTo(startingHeight);

        isMoving = false;
        yield return new WaitForSeconds(2f); // Visszatérés után vár
        DecideAndAct();
    }

    float GetPostShotDelay()
    {
        return enemyType == EnemyType.Sniper ? sniperHitDelay : duckTime;
    }

    IEnumerator FireAtPlayer()
    {

        if (GameManager.Instance.IsHiding())
        {
            Debug.Log("Player is hiding, enemy will not shoot.");
            yield break; // Ha a játékos bújik, nem kezdjük el a lövést
        }

        if (enemyType == EnemyType.Sniper && shine != null)
        {
            StartCoroutine(ShineWarningAfterDelay(Mathf.Max(0f, firingTimer - shineWarningBeforeFirst)));
            StartCoroutine(ShineWarningAfterDelay(Mathf.Max(0f, firingTimer - shineWarningBeforeSecond)));
        }

        // Lövés előtt fireFrameLeadTime másodperccel már a lövő pózra vált, és ott is marad lövésig.
        float leadTime = Mathf.Clamp(fireFrameLeadTime, 0f, firingTimer);
        yield return new WaitForSeconds(firingTimer - leadTime);

        PlaySpriteAnimation(FireFrameSequence, frameDuration, false);
        yield return new WaitForSeconds(leadTime);

        // Aktiváljuk a spark-ot lövés előtt
        spark.SetActive(true);
        if (enemyType == EnemyType.Sniper)
        {
            GameManager.Instance.PlaySniperGunshotSound();
        }
        else
        {
            GameManager.Instance.PlayGunshotSound();
        }
        yield return new WaitForSeconds(0.2f); // A lövés ideje
        spark.SetActive(false);

        // Csak ezután kezdődik a guggolás - snipernél hosszabb a késleltetés, mint sima katonánál.
        yield return new WaitForSeconds(Mathf.Max(0f, GetPostShotDelay() - 0.2f));

        if (GameManager.Instance.IsHiding())
        {
            GameManager.Instance.PlayPlayerMissSound();
            yield break; // Ha a játékos bújik, a lövés nem talál
        }

    if (IsDead)
    {
        yield break;
    }
    if (Mathf.Abs(transform.localPosition.y - targetHeight) > 0.2f)
    {
        yield break;
    }

    if (enemyType == EnemyType.Soldier)
    {
        bool hitPlayer = Random.value <= GameManager.Instance.GetSoldierHitChance();

        if (hitPlayer)
        {
            GameManager.Instance.TakeDamage(1);
        }
        else
        {
            GameManager.Instance.PlayPlayerMissSound();

        }
    }
    else if (enemyType == EnemyType.Sniper)
    {
        GameManager.Instance.TakeDamage(3);
    }

    EnemyManager.Instance.ResetEnemy(this);
    }

    IEnumerator ShineWarningAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        yield return PlayShineAnimation();
    }

    IEnumerator PlayShineAnimation()
    {
        shine.transform.localScale = Vector3.one * shineMinScale;
        shine.transform.localRotation = Quaternion.identity;
        shine.SetActive(true);

        float elapsed = 0f;
        while (elapsed < shineAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shineAnimationDuration);

            float scale = t < 0.5f
                ? Mathf.Lerp(shineMinScale, shineMaxScale, t / 0.5f)
                : Mathf.Lerp(shineMaxScale, shineMinScale, (t - 0.5f) / 0.5f);

            shine.transform.localScale = Vector3.one * scale;
            shine.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, shineRotationDegrees, t));

            yield return null;
        }

        shine.transform.localScale = Vector3.one * shineMinScale;
        shine.transform.localRotation = Quaternion.identity;
        shine.SetActive(false);
    }

    public void SetEnemyType(EnemyType newType)
    {
        enemyType = newType;
    }
    public void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        IsKillable = false;
        OnDeathDelegate?.Invoke();

        StopAllCoroutines();

        if (shine != null && shine.activeSelf)
        {
            shine.SetActive(false);
        }

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        PlaySpriteAnimation(DeathFrameSequence, deathFrameDuration, false);
        yield return MoveTo(startingHeight, deathSinkSpeed);
        Destroy(gameObject);
    }
}
