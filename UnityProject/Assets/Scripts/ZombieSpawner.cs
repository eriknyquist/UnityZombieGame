/*
 * ZombieSpawner.cs
 *
 * Attach to a gameobject placed at the position you want zombies to spawn from.
 * Provides an interface to spawn a configured prefab with a random intial rotation.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab;

    public int spawned = 0;

    public void SpawnZombie()
    {
        GameObject spawnedZombie = Instantiate(zombiePrefab,
                                               (Vector2) gameObject.transform.position,
                                               Quaternion.identity);
        Zombie zombie = spawnedZombie.GetComponent<Zombie>();
        zombie.SetRotationAngle(Random.Range(0f, 360f));
        spawned += 1;
    }
}
