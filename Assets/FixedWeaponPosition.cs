using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FixedWeaponPosition : MonoBehaviour {
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

    bool isActive = false;
    bool isReloading = false;
    bool reloadEnded = false;

    
    private float startY = -3f;
    private float targetY = -7f;

    private float currentT;

    List<Transform> GetEnemies => FindObjectsByType<SoldierMovement>(FindObjectsSortMode.None).Select(s => s.transform).ToList();

    void Start() {
        if (cam == null) cam = Camera.main;
        enemySoldiers = GameManager.Instance.GetEnemies();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isActive && GameManager.Instance.CanShoot())
        {
            StartCoroutine(FireEffect());
            CheckIfEnemyGotShot();
        }

        if(GameManager.Instance.GetSide()){
            spriteRenderer.sprite = hungarianWeapon;
        }else{
            spriteRenderer.sprite = russianWeapon;
        }

        if (cam == null) cam = Camera.main;
        float weaponYOffset = 0f;
        
        if (GameManager.Instance.IsHiding() && isReloading) {
            weaponYOffset = -10f;  
        }
        
        Vector3 dynamicOffset = viewportOffset;
        dynamicOffset.y += weaponYOffset;
        
        if(isReloading){
            float newY;
            if(currentT < 2f) {
                float downProgress = currentT / 2f; 
                newY = Mathf.Lerp(startY, targetY, downProgress);
            } else if(currentT < 4f) {
                newY = targetY; 
            } else {
                float upProgress = (currentT - 4f) / 2f; 
                newY = Mathf.Lerp(targetY, startY, upProgress);
            }
            
            Vector3 pos = transform.position;
            pos.y = newY + weaponYOffset;  
            transform.position = pos;
            
            currentT += Time.deltaTime * lerpSpeed;
            if (currentT >= 6f) { 
                isReloading = false;
                currentT = 0f;       
                isActive = false;  
            }
        } else {
            transform.position = cam.ViewportToWorldPoint(dynamicOffset);
            transform.LookAt(cam.transform); 
            transform.Rotate(0, 180, 0);
        }
    }

    void LateUpdate() {
        
    }

    IEnumerator FireEffect()
    {
        isActive = true; 
        //fireSpark.active = true;
        fireSound.Play();
        sparkAnimator.SetTrigger("Fire");

        yield return new WaitForSeconds(0.3f);

        //fireSpark.active = false;
        isReloading = true;
    }

    void CheckIfEnemyGotShot()
    {
        foreach (Transform enemy in GetEnemies)
        {
            if (enemy == null) continue;

            var soldier = enemy.GetComponent<SoldierMovement>();
            if (soldier.IsDead || !soldier.IsKillable) continue;

            float distance = Vector3.Distance(new Vector3(fireSpark.transform.position.x, fireSpark.transform.position.y, 0f), new Vector3(enemy.transform.position.x, fireSpark.transform.position.y, 0f));
            if(distance < 0.3)
            {
                Debug.Log("BUMMM");
                soldier.Die();
            }
        }
    }
}
