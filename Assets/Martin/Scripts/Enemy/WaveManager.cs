using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Usar solo en salas que sean oleadas. Las salas normales siguen con SpawnerManager.
public class WaveManager : MonoBehaviour
{
    [Serializable]
    public class EnemyWave
    {
        public string waveName = "Oleada";
        public List<EnemySpawner> spawners = new();
    }

    [SerializeField] private List<EnemyWave> waves = new();
    [SerializeField] private float delayBetweenWaves = 2f;

    // Las salas normales arrancan solas cuando entra el jugador.
    // Para las oleadas que invoca un boss hay que apagarlo: las lanza el boss, no el trigger.
    [SerializeField] private bool startOnPlayerEnter = true;

    public event Action OnAllWavesCleared;

    public int CurrentWave { get; private set; } = -1;
    public bool IsRunning { get; private set; }

    // true cuando las oleadas ya arrancaron y no queda ningun enemigo vivo
    public bool AllCleared => hasStarted && !IsRunning;

    public int WaveCount => waves.Count;

    private bool hasStarted;

    private void OnTriggerEnter(Collider other)
    {
        if (!startOnPlayerEnter) return;
        if (!other.CompareTag("Player")) return;

        StartWaves();
    }

    public void StartWaves()
    {
        if (hasStarted || waves.Count == 0) return;

        hasStarted = true;
        StartCoroutine(WaveLoop());
    }

    // Deja las oleadas listas para volver a lanzarse, por si el boss repite el ataque.
    // No destruye a los enemigos que sigan vivos de la tanda anterior.
    public void ResetWaves()
    {
        StopAllCoroutines();

        IsRunning = false;
        hasStarted = false;
        CurrentWave = -1;
    }

    private IEnumerator WaveLoop()
    {
        IsRunning = true;

        for (int i = 0; i < waves.Count; i++)
        {
            CurrentWave = i;
            Debug.Log($"[Oleadas] Inicia {waves[i].waveName}");

            SpawnWave(waves[i]);

            while (!IsWaveCleared(waves[i]))
            {
                yield return null;
            }

            Debug.Log($"[Oleadas] {waves[i].waveName} terminada");

            yield return new WaitForSeconds(delayBetweenWaves);
        }

        IsRunning = false;
        CurrentWave = waves.Count;

        OnAllWavesCleared?.Invoke();
    }

    private void SpawnWave(EnemyWave wave)
    {
        foreach (EnemySpawner spawner in wave.spawners)
        {
            if (spawner != null) spawner.SpawnAll();
        }
    }

    private bool IsWaveCleared(EnemyWave wave)
    {
        foreach (EnemySpawner spawner in wave.spawners)
        {
            if (spawner != null && spawner.AliveCount > 0) return false;
        }

        return true;
    }
}