using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Contador de enemigos por nivel: total automático, derrotados persistentes y eventos para UI y compuertas.
// Colocar UN solo componente en la escena persistente (Game.unity).
public class LevelObjectiveManager : MonoBehaviour
{
    public static LevelObjectiveManager Instance { get; private set; }

    public event Action<string> OnLevelEntered;                 // levelId
    public event Action<string, int, int> OnProgressChanged;    // levelId, derrotados, requeridos
    public event Action<string> OnThresholdReached;             // levelId
    public event Action<string, int> OnExitBlocked;             // levelId, enemigos que faltan
    public event Action<string> OnBossEncounterStarted;         // levelId
    public event Action<string> OnBossDefeated;                 // levelId

    private sealed class LevelState
    {
        public LevelConfig config;
        public int total;
        // Derrotados. Incluye kills de encuentros pendientes (resettable).
        public HashSet<string> defeated = new HashSet<string>();
        // Kills de encuentros que se revierten si el jugador muere antes de completarlos.
        public HashSet<string> resettable = new HashSet<string>();
        public bool thresholdReached;
        public bool bossStarted;
        public bool bossDefeated;
    }

    private readonly Dictionary<string, LevelState> levels = new Dictionary<string, LevelState>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Sobrevive a la descarga de la escena donde se colocó (los SceneLoader descargan las zonas).
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        PlayerControl.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        PlayerControl.OnPlayerDied -= HandlePlayerDied;
    }

    // =====================================================================
    // REGISTRO DE SPAWNS
    // =====================================================================

    // Llamar ANTES de enemy.OnSpawn(...), desde el componente que instancia al enemigo.
    // scene: escena del componente que spawnea (no la del enemigo, que puede moverse a otra escena).
    // counts: si el enemigo cuenta para el porcentaje del nivel.
    // resetsOnDeath: si el encuentro se reinicia cuando el jugador muere (ambush, ascensor, tutorial).
    public static void AssignObjective(EnemyBase enemy, Scene scene, string sourceKey, int index, bool counts, bool resetsOnDeath)
    {
        if (enemy == null) return;

        bool hasLevel = LevelZone.TryGetConfig(scene, out LevelConfig config);

        if (counts && !hasLevel)
            Debug.LogWarning($"[LevelObjective] {enemy.name} marcado como contable, pero la escena '{scene.name}' no tiene LevelZone.", enemy);

        bool valid = counts && hasLevel;
        string objectiveId = valid ? config.LevelId + "|" + sourceKey + "|" + index : null;

        enemy.ConfigureObjective(valid ? config.LevelId : null, objectiveId, valid, resetsOnDeath);
    }

    // Ruta de jerarquía estable que identifica al componente que genera enemigos.
    public static string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    // =====================================================================
    // ENTRADA AL NIVEL
    // =====================================================================

    // Llamado por LevelZone al entrar el jugador.
    public void EnterLevel(LevelConfig config, GameObject zoneRoot)
    {
        if (config == null || zoneRoot == null) return;

        if (!levels.TryGetValue(config.LevelId, out LevelState state))
        {
            state = new LevelState
            {
                config = config,
                total = CountSpawnSources(zoneRoot.scene)
            };

            LoadSavedKills(state);
            levels.Add(config.LevelId, state);
        }

        OnLevelEntered?.Invoke(config.LevelId);
        Notify(state);
        CheckThreshold(state);
    }

    private static int CountSpawnSources(Scene scene)
    {
        int total = 0;

        // Include: los spawners pueden estar desactivados hasta que entra el jugador.
        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (MonoBehaviour mb in all)
        {
            if (mb is ILevelSpawnSource source && mb.gameObject.scene == scene)
                total += source.CountLevelEnemies();
        }

        return total;
    }

    private static void LoadSavedKills(LevelState state)
    {
        // Partida nueva: no se cargan kills viejos.
        if (!SaveLoadManager.canLoad) return;

        // Se lee el archivo directamente para no disparar los efectos secundarios de LoadGame().
        string path = Path.Combine(Application.persistentDataPath, "savefile.json");
        if (!File.Exists(path)) return;

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        if (data == null || data.levelKills == null) return;

        foreach (LevelKillsSavedEntry entry in data.levelKills)
        {
            if (entry.levelId == state.config.LevelId && entry.defeatedIds != null)
                state.defeated = new HashSet<string>(entry.defeatedIds);
        }
    }

    // =====================================================================
    // KILLS
    // =====================================================================

    // Llamado desde EnemyBase.Die(). Cada objectiveId cuenta una sola vez.
    public void NotifyEnemyDied(EnemyBase enemy)
    {
        if (enemy == null || !enemy.CountsForLevel || enemy.ObjectiveId == null) return;
        if (!levels.TryGetValue(enemy.LevelId, out LevelState state)) return;
        if (!state.defeated.Add(enemy.ObjectiveId)) return;

        if (enemy.ResetsOnPlayerDeath) state.resettable.Add(enemy.ObjectiveId);

        SaveAll();
        Notify(state);
        CheckThreshold(state);
    }

    // Llamar SOLO cuando el encuentro se completa (no en reset). Sus kills dejan de revertirse.
    public void CommitEncounter(string sourceKey)
    {
        foreach (LevelState state in levels.Values)
            state.resettable.RemoveWhere(id => SourceOf(id) == sourceKey);

        SaveAll();
    }

    private void HandlePlayerDied()
    {
        bool changed = false;

        foreach (LevelState state in levels.Values)
        {
            if (state.resettable.Count == 0) continue;

            state.defeated.ExceptWith(state.resettable);
            state.resettable.Clear();
            changed = true;
            Notify(state);
        }

        if (changed) SaveAll();
    }

    private static string SourceOf(string objectiveId)
    {
        string[] parts = objectiveId.Split('|');
        return parts.Length >= 2 ? parts[1] : null;
    }

    // =====================================================================
    // UMBRAL Y CONSULTAS
    // =====================================================================

    private static int RequiredFor(LevelState state)
    {
        return state.config.RequiredKills(state.total);
    }

    private void CheckThreshold(LevelState state)
    {
        if (state.thresholdReached) return;
        if (state.defeated.Count < RequiredFor(state)) return;

        state.thresholdReached = true;
        Notify(state);
        OnThresholdReached?.Invoke(state.config.LevelId);
    }

    private void Notify(LevelState state)
    {
        OnProgressChanged?.Invoke(state.config.LevelId, state.defeated.Count, RequiredFor(state));
    }

    public bool TryGetProgress(string levelId, out int defeated, out int required)
    {
        defeated = 0;
        required = 0;

        if (!levels.TryGetValue(levelId, out LevelState state)) return false;

        defeated = state.defeated.Count;
        required = RequiredFor(state);
        return true;
    }

    // Una vez cumplido el umbral, la salida queda abierta aunque después mueran más enemigos.
    public bool IsUnlocked(string levelId)
    {
        return levels.TryGetValue(levelId, out LevelState state) && state.thresholdReached;
    }

    public void NotifyExitBlocked(string levelId)
    {
        if (!TryGetProgress(levelId, out int defeated, out int required)) return;
        OnExitBlocked?.Invoke(levelId, Mathf.Max(0, required - defeated));
    }

    // =====================================================================
    // BOSS
    // =====================================================================

    public void NotifyBossEncounterStarted(string levelId)
    {
        if (!levels.TryGetValue(levelId, out LevelState state) || state.bossStarted) return;

        state.bossStarted = true;
        OnBossEncounterStarted?.Invoke(levelId);
    }

    public void NotifyBossDefeated(string levelId)
    {
        if (!levels.TryGetValue(levelId, out LevelState state) || state.bossDefeated) return;

        state.bossDefeated = true;
        OnBossDefeated?.Invoke(levelId);
    }

    // =====================================================================
    // GUARDADO
    // =====================================================================

    private void SaveAll()
    {
        if (SaveLoadManager.Instance == null) return;

        // Solo se guardan kills que no están pendientes de un encuentro.
        var entries = new List<LevelKillsSavedEntry>();

        foreach (LevelState state in levels.Values)
        {
            entries.Add(new LevelKillsSavedEntry
            {
                levelId = state.config.LevelId,
                defeatedIds = state.defeated.Where(id => !state.resettable.Contains(id)).ToList()
            });
        }

        SaveLoadManager.Instance.SaveGame("levelKills", entries);
    }
}
