using UnityEngine;

public class CharSelector_Manager : MonoBehaviour
{
    public static CharSelector_Manager Instance;

    [Header("Character data list")]
    [SerializeField] private CharacterInfo[] characters;

    [Header("Initial Spawn point")]
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        Instance = this;    
    }

    public CharacterInfo GetCharacterInfo(int index)
    {
        if ( index < 0 || index >= characters.Length) return null;

        return characters[index];
    }

    public Transform GetInitialSpawnPoint()
    {
        return spawnPoint;
    }

    public void SetupRespawn()
    {
        // ✅ GUARDAR EL SPAWN INICIAL EN RESPAWNMANAGER
        if (RespawnManager.Instance != null && spawnPoint != null)
        {
            RespawnManager.Instance.SetInitialSpawnPoint(spawnPoint);
            Debug.Log("✅ Initial spawn point configurado en RespawnManager");
        }
    }
}
