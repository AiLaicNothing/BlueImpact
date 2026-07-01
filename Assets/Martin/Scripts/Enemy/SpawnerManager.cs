using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [SerializeField] private List<EnemySpawner> spawnEntries = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            for (int i = 0; i < spawnEntries.Count; i++)
            {
                if (!spawnEntries[i].hasSpawnedAlready) spawnEntries[i].SpawnEnemies();
            }
        }
    }
}
