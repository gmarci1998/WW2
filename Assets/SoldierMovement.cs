using UnityEngine;
using System.Collections;
using System;
using Random = UnityEngine.Random;  // Coroutine-hez

public class SoldierMovement : MonoBehaviour
{
    [SerializeField] private float enemyParallaxX = 4f;
    [SerializeField] private float enemyParallaxY = 0.2f;
    [SerializeField] private float moveSpeed = 2f;
    private float targetHeight;
    private bool isMoving = false;
    private float waitForShooting = 4f;
    private float duckTime = 2f;
    private float firingTimer = 0f;
    private GameObject spark;

    [SerializeField] private float startingHeight = 0f;
    [SerializeField] private float maximumHeight = 3f;
    private int nextAction;
    private bool actionComplete = false;

    [SerializeField] private AudioSource dying;
    [SerializeField] private float timeBeforeFirstAction = 3f;

    public bool IsDead { get; set; } = false;

    public Action OnDeathDelegate { get; set; }

    void Start()
    {
        startingHeight = transform.position.y;
        maximumHeight = startingHeight + 0.30f;

        StartCoroutine(DecideAndActDelayed());
        spark = transform.GetChild(0).gameObject;
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

        if (nextAction == 0)
        {
            // Akció 0: Leguggolva marad, majd újra dönt
            StartCoroutine(StayCrouchedAndDecideAgain());
        }
        else if (nextAction == 1)
        {
            // Akció 1: Félig előbújik, majd visszamegy és újra dönt
            targetHeight = startingHeight + (maximumHeight - startingHeight) / 2f; // Pontosítva a targetHeight
            StartCoroutine(PeekAndReturn());
        }
        else if (nextAction == 2)
        {
            // Akció 2: Teljesen előbújik, majd visszamegy és újra dönt
            targetHeight = maximumHeight; // Pontosítva a targetHeight
            StartCoroutine(FullyEmergeAndReturn());
        }
    }

    int DecideAction()
    {
        return Random.Range(0, 3); // Három akció közül választ
    }

    IEnumerator StayCrouchedAndDecideAgain()
    {
        yield return new WaitForSeconds(Random.Range(2f, 5f));
        DecideAndAct();
    }

    IEnumerator PeekAndReturn()
    {
        isMoving = true;
        while (transform.position.y < targetHeight)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.position += Vector3.up * moveSpeed * deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(2f); // Rövid időt marad félig előbújva

        while (Mathf.Abs(transform.position.y - startingHeight) > 0.01f)
        { // Tolerancia hozzáadása
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.position -= Vector3.up * moveSpeed * deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.position = new Vector3(transform.position.x, startingHeight, transform.position.z); // Biztosítjuk, hogy pontosan az alaphelyzetbe kerüljön
        isMoving = false;
        yield return new WaitForSeconds(1f); // Visszatérés után vár
        DecideAndAct();
    }

    IEnumerator FullyEmergeAndReturn()
    {
        isMoving = true;
        while (transform.position.y < targetHeight)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.position += Vector3.up * moveSpeed * deltaTime;
            yield return null;
        }

        float waitingTime = Random.Range(2f, 10f);
        firingTimer = Random.Range(2f, waitingTime);

        if (firingTimer + duckTime > waitingTime)
        {
            firingTimer = waitingTime - duckTime - 0.1f; // Biztosítjuk, hogy legyen idő guggolni
        }

        StartCoroutine(FireAtPlayer());

        yield return new WaitForSeconds(waitingTime); // Teljesen előbújva marad egy ideig

        while (Mathf.Abs(transform.position.y - startingHeight) > 0.01f)
        { // Tolerancia hozzáadása
            float deltaTime = Mathf.Max(Time.deltaTime, 0.01f); // Minimális deltaTime érték beállítása
            transform.position -= Vector3.up * moveSpeed * deltaTime;
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, startingHeight, transform.position.z); // Biztosítjuk, hogy pontosan az alaphelyzetbe kerüljön
        isMoving = false;
        yield return new WaitForSeconds(2f); // Visszatérés után vár
        DecideAndAct();
    }

    IEnumerator FireAtPlayer()
    {
        Debug.Log("Preparing to Shoot in: " + firingTimer + " seconds");
        yield return new WaitForSeconds(firingTimer);

        // Aktiváljuk a spark-ot lövés előtt
        spark.SetActive(true);
        yield return new WaitForSeconds(0.2f); // A lövés ideje
        spark.SetActive(false);

        // Csak ezután kezdődik a guggolás
        yield return new WaitForSeconds(duckTime - 0.2f);

        if (GameManager.Instance.IsHiding())
        {
            Debug.Log("Player is hiding, enemy shot missed.");
            yield break; // Ha a játékos bújik, a lövés nem talál
        }

        if (IsDead)
        {
            Debug.Log("Died before firing.");
            yield break;
        }

        GameManager.Instance.PlayerDeath();
        EnemyManager.Instance.ResetAllEnemies();
    }

    public void Die()
    {
        Debug.Log("Enemy Soldier Died");
        IsDead = true;
        OnDeathDelegate?.Invoke();
        dying.Play();
        Destroy(gameObject, 0.2f);
    }
}
