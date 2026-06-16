// En PlayerSpawn_Manager.cs
using System;
using UnityEngine;

public class PlayerSpawn_Manager : MonoBehaviour
{
    public static PlayerSpawn_Manager Instance;
    public static event Action<PlayerControl> OnPlayerSpawned;  // ✅ AGREGA ESTO

    private CharacterInfo currentCharacterInfo;

    private void Awake()
    {
        Instance = this;
    }

    public void SetCharacter(CharacterInfo info)
    {
        currentCharacterInfo = info;
    }

    public void SpawnCharacter(Transform spawnPoint)
    {
        var charPrefab = currentCharacterInfo.prefab;
        var playerInstance = Instantiate(charPrefab, spawnPoint.position, Quaternion.identity);

        var playerControl = playerInstance.GetComponent<PlayerControl>();
        if (playerControl != null)
        {
            // ✅ DISPARAR EVENTO
            OnPlayerSpawned?.Invoke(playerControl);
        }
    }
}