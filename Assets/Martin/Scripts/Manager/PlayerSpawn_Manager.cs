// En PlayerSpawn_Manager.cs
using System;
using UnityEngine;

public class PlayerSpawn_Manager : MonoBehaviour
{
    public static PlayerSpawn_Manager Instance;
    public PlayerControl CurrentPlayerControl { get; private set; }
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
        if (currentCharacterInfo == null || currentCharacterInfo.prefab == null)
        {
            Debug.LogError("[PlayerSpawn] No hay CharacterInfo o prefab asignado.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[PlayerSpawn] El punto de spawn es nulo.");
            return;
        }

        var playerInstance = Instantiate(currentCharacterInfo.prefab, spawnPoint.position, Quaternion.identity);

        if (RespawnManager.Instance != null) RespawnManager.Instance.SetPlayerTarget(playerInstance);
        if (Player_Manager.Instance != null) Player_Manager.Instance.SetPlayer(playerInstance);

        var playerControl = playerInstance.GetComponent<PlayerControl>();

        CurrentPlayerControl = playerControl;

        if (playerControl != null)
        {
            playerControl.CurrentCharacterInfo = currentCharacterInfo;
            OnPlayerSpawned?.Invoke(playerControl);
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetMode(GameMode.Gameplay);
        }
        else
        {
            Debug.LogWarning("⚠️ GameModeManager no encontrado.");
        }
    }
}
