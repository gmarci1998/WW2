using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefabs;
    
    public static EnemyManager Instance { get; private set; }

    List<SoldierMovement> soldiers = new();
    GameObject[] spawnPoints;
    GameObject parent;

    void Awake()
    {
        Init();
        SpawnEnemies();
    }

    private void Init()
    {
        Instance = this;

        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        // Keressük meg a Mountains GameObjectet
        parent = GameObject.Find("Mountains");

        DontDestroyOnLoad(gameObject);
    }

    void SpawnEnemies()
    {
        // Minden SpawnPoint-hoz spawnoljunk egy enemyPrefab példányt
        foreach (var spawnPoint in spawnPoints)
        {
            GameObject enemy = Instantiate(enemyPrefabs, spawnPoint.transform.position, spawnPoint.transform.rotation, parent.transform);

            // Ellenőrizzük, hogy van-e SoldierMovement komponens, és adjuk hozzá a listához
            SoldierMovement soldierMovement = enemy.GetComponent<SoldierMovement>();
            if (soldierMovement != null)
            {
                soldiers.Add(soldierMovement);
                soldierMovement.OnDeathDelegate += () =>
                {
                    if(soldiers.Contains(soldierMovement))
                    {
                        soldiers.Remove(soldierMovement);
                        StartCoroutine(RespawnEnemy(spawnPoint.gameObject.transform));
                    }
                };
            }
            else
            {
                Debug.LogWarning("A létrehozott enemyPrefab nem tartalmaz SoldierMovement komponenst!");
            }
        }
    }

    IEnumerator RespawnEnemy(Transform parent)
    {
        yield return new WaitForSeconds(Random.Range(3, 10));
        GameObject enemy = Instantiate(enemyPrefabs, transform.position, transform.rotation, parent.transform);

        soldiers.Add(enemy.GetComponent<SoldierMovement>());
    }

    public void ResetAllEnemies()
    {
        foreach (var enemy in soldiers)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject); // Töröljük az enemy GameObjectet
            }
        }
        soldiers.Clear(); // Tisztítsuk meg a listát
        SpawnEnemies(); // Újra spawnoljuk az enemy-kat
    }

    void Update()
    {
        
    }
}
