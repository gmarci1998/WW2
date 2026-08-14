using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject soldierPrefab;
    [SerializeField] private GameObject soldierPrefab2;
    [SerializeField] private GameObject sniperPrefab;

    [System.Serializable]
    public class SpawnPointCoverConfig
    {
        public string coverName; 
        public float riseAmount; 
    }

    [SerializeField] private float defaultRiseAmount = 1.65f; 
    [SerializeField]
    private SpawnPointCoverConfig[] coverConfigs = new SpawnPointCoverConfig[]
    {
        new SpawnPointCoverConfig { coverName = "Ground", riseAmount = 1.65f },
        new SpawnPointCoverConfig { coverName = "Ground (3)", riseAmount = 1.65f },
        new SpawnPointCoverConfig { coverName = "Ground (2)", riseAmount = 2.05f },
        new SpawnPointCoverConfig { coverName = "Ground (1)", riseAmount = 2.94f },
    };

    public static EnemyManager Instance { get; private set; }

    List<SoldierMovement> soldiers = new();
    Dictionary<Transform, SoldierMovement> occupiedSpawnPoints = new(); 
    GameObject[] spawnPoints;
    GameObject[] spawnPointsSide;
    GameObject parent;

    void Awake()
    {
        Init();
        SpawnEnemies();
    }

    private void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;

        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPointsSide = GameObject.FindGameObjectsWithTag("SpawnPointSide");
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
            SpawnOneEnemy(spawnPoint.transform);
        }

        foreach (var spawnPoint in spawnPointsSide)
        {
            SpawnOneEnemy(spawnPoint.transform);
        }
    }

    void SpawnOneEnemy(Transform spawnPoint)
    {
        if (occupiedSpawnPoints.TryGetValue(spawnPoint, out SoldierMovement existing) && existing != null)
        {
            return;
        }

        GameObject prefabToSpawn = GetRandomEnemyPrefab();
        GameObject enemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation, parent.transform);

        SoldierMovement soldierMovement = enemy.GetComponent<SoldierMovement>();
        if (soldierMovement == null)
        {
            return;
        }

        soldierMovement.SetEnemyType(prefabToSpawn == sniperPrefab ? SoldierMovement.EnemyType.Sniper : SoldierMovement.EnemyType.Soldier);
        soldierMovement.SetPeekRiseAmount(GetRiseAmountForSpawnPoint(spawnPoint));

        soldiers.Add(soldierMovement);
        occupiedSpawnPoints[spawnPoint] = soldierMovement;

        soldierMovement.OnDeathDelegate += () =>
        {
            if (soldiers.Contains(soldierMovement))
            {
                soldiers.Remove(soldierMovement);
            }

            if (occupiedSpawnPoints.TryGetValue(spawnPoint, out SoldierMovement current) && current == soldierMovement)
            {
                occupiedSpawnPoints.Remove(spawnPoint);
            }

            StartCoroutine(RespawnEnemy(spawnPoint));
        };
    }

    float GetRiseAmountForSpawnPoint(Transform spawnPoint)
    {
        SpawnPointCoverInfo coverInfo = spawnPoint.GetComponent<SpawnPointCoverInfo>();
        if (coverInfo != null)
        {
            return coverInfo.riseAmount;
        }

        string coverName = spawnPoint.parent?.name;

        if (coverName != null)
        {
            foreach (var config in coverConfigs)
            {
                if (config.coverName == coverName)
                {
                    return config.riseAmount;
                }
            }
        }

        return defaultRiseAmount;
    }

    IEnumerator RespawnEnemy(Transform spawnPoint)
    {
        yield return new WaitForSeconds(Random.Range(10, 20));
        SpawnOneEnemy(spawnPoint);
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
        occupiedSpawnPoints.Clear();
        SpawnEnemies(); // Újra spawnoljuk az enemy-kat
    }

    public void ResetEnemy(SoldierMovement enemy)
    {
        if (enemy == null)
        {
            return;
        }

        Transform spawnPoint = null;
        foreach (var kvp in occupiedSpawnPoints)
        {
            if (kvp.Value == enemy)
            {
                spawnPoint = kvp.Key;
                break;
            }
        }

        soldiers.Remove(enemy);

        if (spawnPoint != null)
        {
            occupiedSpawnPoints.Remove(spawnPoint);
        }

        Destroy(enemy.gameObject);

        if (spawnPoint != null)
        {
            SpawnOneEnemy(spawnPoint);
        }
    }

    void Update()
    {
        
    }
}
