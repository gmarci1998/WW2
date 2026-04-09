using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject soldierPrefab;
    [SerializeField] private GameObject sniperPrefab;
    
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
    GameObject GetRandomEnemyPrefab()
    {
        int roll = Random.Range(0, 8);

        if (roll == 0)
        {
            return sniperPrefab;
        }

        return soldierPrefab;
    }

    void SpawnEnemies()
    {
        // Minden SpawnPoint-hoz spawnoljunk egy enemyPrefab példányt
        foreach (var spawnPoint in spawnPoints)
        {
            GameObject prefabToSpawn = GetRandomEnemyPrefab();
            GameObject enemy = Instantiate(prefabToSpawn, spawnPoint.transform.position, spawnPoint.transform.rotation, parent.transform);

            SoldierMovement soldierMovement = enemy.GetComponent<SoldierMovement>();
            if (soldierMovement != null)
            {
                if (prefabToSpawn == sniperPrefab)
                {
                    soldierMovement.SetEnemyType(SoldierMovement.EnemyType.Sniper);
                }
                else
                {
                    soldierMovement.SetEnemyType(SoldierMovement.EnemyType.Soldier);
                }

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
        }
    }

    IEnumerator RespawnEnemy(Transform parent)
    {
        yield return new WaitForSeconds(Random.Range(10, 20));
        GameObject prefabToSpawn = GetRandomEnemyPrefab();
        GameObject enemy = Instantiate(prefabToSpawn, parent.position, parent.rotation, this.parent.transform);

        SoldierMovement soldierMovement = enemy.GetComponent<SoldierMovement>();
        if (soldierMovement != null)
        {
            if (prefabToSpawn == sniperPrefab)
            {
                soldierMovement.SetEnemyType(SoldierMovement.EnemyType.Sniper);
            }
            else
            {
                soldierMovement.SetEnemyType(SoldierMovement.EnemyType.Soldier);
            }

            soldiers.Add(soldierMovement);

            soldierMovement.OnDeathDelegate += () =>
            {
                if (soldiers.Contains(soldierMovement))
                {
                    soldiers.Remove(soldierMovement);
                    StartCoroutine(RespawnEnemy(parent));
                }
            };
        }
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
