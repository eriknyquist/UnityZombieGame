using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelManager : MonoBehaviour
{
    public List<GameObject> spawnPoints = new List<GameObject>();
    
    // Delay in seconds between rounds
    public float roundDelaySecs = 2;
    
    // Delay between each new zombie spawn
    public float zombieSpawnDelaySecs = 0.1f;
    
    int maxActiveZombies = 0;
    int maxTotalSpawns = 0; // Total number of zombies spawns allowed for this round
    int totalSpawns = 0;    // Total number of zombies spawned this level
    int activeZombies = 0;  // Current number of zombies spawned
    int level = 0;          // Current level
    int kills = 0;          // Number of kills so far in this level
    
    int spawnPointIndex = 0;
    
    object[][] LevelData = new object[][] {
        //            Total zombie     Max. zombies     Max. ammo
        //            spawns           at once
        //new object[] {50,              10,              20},
        //new object[] {100,             20,              40},
        //new object[] {200,             30,              50},
        //new object[] {300,             50,              70},
        //new object[] {400,             75,              100},
        //new object[] {500,             100,             150},
        //new object[] {600,             150,             200},
        //new object[] {700,             200,             200},
        new object[] {1000,            200,             250},
    };
    
    PlayerHUD playerHUD;

    // Start is called before the first frame update
    void Start()
    {
        GameObject hud = GameObject.FindGameObjectWithTag("PlayerHUD");
        playerHUD = hud.GetComponent<PlayerHUD>();
        
        foreach (var obj in spawnPoints)
        {
        }
        
        maxTotalSpawns = (int)LevelData[0][0];
        maxActiveZombies = (int)LevelData[0][1];
        int maxAmmo = (int)LevelData[0][2];
        
        playerHUD.ammoCounter.maxAmmo = maxAmmo;
        InvokeRepeating("ZombieSpawnTask", 0, zombieSpawnDelaySecs);
    }

    void ZombieSpawnTask()
    {
        if ((activeZombies >= maxActiveZombies) || (totalSpawns >= maxTotalSpawns))
        {
            // Nothing to do, can't spawn anymore zombies
            CancelInvoke("ZombieSpawnTask");
            return;
        }
        
        ZombieSpawner point = spawnPoints[spawnPointIndex].GetComponent<ZombieSpawner>();
        point.SpawnZombie();

        activeZombies += 1;
        totalSpawns += 1;
        spawnPointIndex = (spawnPointIndex + 1) % spawnPoints.Count;
    }
    
    public void ZombieKilled()
    {
        kills += 1;
        activeZombies -= 1;
        
        // Was the last zombie killed?
        if ((int)LevelData[level][0] == kills)
        {
            Invoke("LevelUp", roundDelaySecs);
        }
        else if (!IsInvoking("ZombieSpawnTask"))
        {
            InvokeRepeating("ZombieSpawnTask", 0, zombieSpawnDelaySecs);
        }
    }
    
    public void LevelUp()
    {
        level = (level + 1) % LevelData.Length;
    
        maxTotalSpawns = (int)LevelData[level][0];
        maxActiveZombies = (int)LevelData[level][1];
        int maxAmmo = (int)LevelData[level][2];

        kills = 0;
        totalSpawns = 0;

        playerHUD.ammoCounter.maxAmmo = maxAmmo;
        InvokeRepeating("ZombieSpawnTask", 0, zombieSpawnDelaySecs);
    }
}
