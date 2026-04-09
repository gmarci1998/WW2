using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FixedWeaponPosition : MonoBehaviour
{
    public static FixedWeaponPosition Instance { get; private set; }
    [SerializeField] private Camera cam;
    [SerializeField] private Vector3 viewportOffset = new Vector3(0.5f, 0.2f, 10f);
    [SerializeField] private GameObject fireSpark;
    [SerializeField] private AudioSource fireSound;
    [SerializeField] private Animator sparkAnimator;
    private List<Transform> enemySoldiers = new List<Transform>();
    private SpriteRenderer spriteRenderer;
    [SerializeField] Sprite hungarianWeapon;
    [SerializeField] Sprite russianWeapon;
    [SerializeField] private float lerpSpeed = 2f;

    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeMagnitude = 0.08f;

    [SerializeField] private float kickbackScale = 1.15f;  
    [SerializeField] private float kickbackDuration = 0.08f; 

    bool isActive = false;
    bool isReloading = false;
    bool reloadEnded = false;

    private float startY = -3f;
    private float targetY = -7f;
    private float currentT;

    private Vector3 originalScale;
    private bool isKickbacking = false;

    List<Transform> GetEnemies => FindObjectsByType<SoldierMovement>(FindObjectsSortMode.None).Select(s => s.transform).ToList();

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        if (cam == null) cam = Camera.main;
        enemySoldiers = GameManager.Instance.GetEnemies();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isActive && GameManager.Instance.CanShoot())
        {
            StartCoroutine(FireEffect());
            CheckIfEnemyGotShot();
        }

        if (GameManager.Instance.GetSide())
        {
            spriteRenderer.sprite = hungarianWeapon;
        }
        else
        {
            spriteRenderer.sprite = russianWeapon;
        }

        if (cam == null) cam = Camera.main;
        float weaponYOffset = 0f;

        if (GameManager.Instance.IsHiding() && isReloading)
        {
            weaponYOffset = -10f;
        }

        Vector3 dynamicOffset = viewportOffset;
        dynamicOffset.y += weaponYOffset;

        if (isReloading)
        {
            float newY;
            if (currentT < 2f)
            {
                float downProgress = currentT / 2f;
                newY = Mathf.Lerp(startY, targetY, downProgress);
            }
            else if (currentT < 4f)
            {
                newY = targetY;
            }
            else
            {
                float upProgress = (currentT - 4f) / 2f;
                newY = Mathf.Lerp(targetY, startY, upProgress);
            }

            Vector3 pos = transform.position;
            pos.y = newY + weaponYOffset;
            transform.position = pos;

            currentT += Time.deltaTime * lerpSpeed;
            if (currentT >= 6f)
            {
                isReloading = false;
                currentT = 0f;
                isActive = false;
            }
        }
        else
        {
            transform.position = cam.ViewportToWorldPoint(dynamicOffset);
            transform.LookAt(cam.transform);
            transform.Rotate(0, 180, 0);
        }
    }

    void LateUpdate() { }

    IEnumerator FireEffect()
    {
        isActive = true;
        fireSound.Play();
        sparkAnimator.SetTrigger("Fire");

        StartCoroutine(WeaponKickback());
        StartCoroutine(ShakeCamera());

        yield return new WaitForSeconds(0.3f);
        isReloading = true;
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