using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    private List<SoldierMovement> enemySoldiers = new ();

    void Start()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("EnemySoldier");
        foreach (GameObject enemy in enemies)
        {
            enemySoldiers.Add(enemy.GetComponent<SoldierMovement>());
        }
    }

    
    void Update()
    {
        
    }
}
