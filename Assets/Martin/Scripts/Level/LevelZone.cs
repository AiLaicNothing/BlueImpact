using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Trigger grande que cubre toda la zona. Una LevelZone por escena de nivel.
// Se registra en Awake (no al entrar) para que los spawners sepan a qué nivel pertenecen
// aunque su trigger se dispare antes que el de la zona.
[RequireComponent(typeof(Collider))]
public class LevelZone : MonoBehaviour
{
    private static readonly Dictionary<int, LevelConfig> configByScene = new Dictionary<int, LevelConfig>();

    [SerializeField] private LevelConfig config;

    public static bool TryGetConfig(Scene scene, out LevelConfig result)
    {
        return configByScene.TryGetValue(scene.handle, out result);
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (config != null) configByScene[gameObject.scene.handle] = config;
    }

    private void OnDestroy()
    {
        configByScene.Remove(gameObject.scene.handle);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (config == null || LevelObjectiveManager.Instance == null) return;
        if (!IsRealPlayer(other)) return;

        LevelObjectiveManager.Instance.EnterLevel(config, gameObject);
    }

    // Jugador real. Excluye el taunt (hereda de PlayerControl) para que no cambie de nivel ni cuente.
    public static bool IsRealPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerControl>() != null
            && other.GetComponentInParent<TauntEnemies>() == null;
    }
}
