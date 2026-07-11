// En PlayerSpawn_Manager.cs
using System;
using UnityEngine;

public class PlayerSpawn_Manager : MonoBehaviour
{
    public static PlayerSpawn_Manager Instance;
    public static event Action<PlayerControl> OnPlayerSpawned;  

    private CharacterInfo currentCharacterInfo;

    private void Awake()
    {
        Instance = this;
    }

    public void SetCharacter(CharacterInfo info)
    {
        currentCharacterInfo = info;
    }

    public CharacterInfo GetCharacter()
    {
        return currentCharacterInfo;
    }

    public void SpawnCharacter(Transform spawnPoint)
    {
        var charPrefab = currentCharacterInfo.prefab;
        var playerInstance = Instantiate(charPrefab, spawnPoint.position, Quaternion.identity);

        var playerControl = playerInstance.GetComponent<PlayerControl>();
        if (playerControl != null)
        {
            playerControl.CurrentCharacterInfo = currentCharacterInfo;
            OnPlayerSpawned?.Invoke(playerControl);
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetMode(GameMode.Gameplay);
            Debug.Log("🎮 Cambiado a GameMode.Gameplay - Cursor bloqueado");
        }
        else
        {
            Debug.LogWarning("⚠️ GameModeManager no encontrado. Inicializando...");
        }
    }
}