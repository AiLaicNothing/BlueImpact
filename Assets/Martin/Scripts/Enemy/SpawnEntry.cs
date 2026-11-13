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

    [Tooltip("Si este spawn cuenta para el porcentaje del nivel. Desmarcar en summons del boss.")]
    public bool countsForLevel = true;

    [HideInInspector] public bool hasSpawned;
    [HideInInspector] public EnemyBase spawnedEnemy;
}
