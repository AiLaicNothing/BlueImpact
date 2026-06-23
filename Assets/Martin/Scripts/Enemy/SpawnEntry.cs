using UnityEngine;

[System.Serializable]
public class SpawnEntry 
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public Transform safePoint;
    public Transform[] patrolPoints;
    public bool hasPatrol;
    public bool spawnOnStart = true;
    public bool oneTimeSpawn = true;

    [HideInInspector] public bool hasSpawned;
    [HideInInspector] public EnemyBase spawnedEnemy;
}
