using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    private List<SoldierMovement> enemySoldiers = new ();
    [SerializeField] private GameObject enemyPrefabs;
    
    public static EnemyManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        // Gyűjtsük ki az összes SpawnPoint nevű GameObjectet
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        // Keressük meg a Mountains GameObjectet
        GameObject mountains = GameObject.Find("Mountains");
        if (mountains == null)
        {
            Debug.LogError("A 'Mountains' GameObject nem található!");
            return;
        }

        // Minden SpawnPoint-hoz spawnoljunk egy enemyPrefab példányt
        foreach (GameObject spawnPoint in spawnPoints)
        {
            GameObject enemy = Instantiate(enemyPrefabs, spawnPoint.transform.position, spawnPoint.transform.rotation);

            // Tegyük az enemy-t a Mountains GameObject child-jává
            enemy.transform.SetParent(mountains.transform);

            // Ellenőrizzük, hogy van-e SoldierMovement komponens, és adjuk hozzá a listához
            SoldierMovement soldierMovement = enemy.GetComponent<SoldierMovement>();
            if (soldierMovement != null)
            {
                enemySoldiers.Add(soldierMovement);
            }
            else
            {
                Debug.LogWarning("A létrehozott enemyPrefab nem tartalmaz SoldierMovement komponenst!");
            }
        }
    }

    public void ClearAllEnemies()
    {
        Debug.Log("Clearing all enemies...");
        foreach (var enemy in enemySoldiers)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject); // Töröljük az enemy GameObjectet
            }
        }
        Debug.Log(enemySoldiers.Count + " enemies destroyed.");
        enemySoldiers.Clear(); // Tisztítsuk meg a listát
        SpawnEnemies(); // Újra spawnoljuk az enemy-kat
    }

    void Update()
    {
        
    }
}
