using UnityEngine;

public class PlayerSpawn_Manager : MonoBehaviour
{ 
    public static PlayerSpawn_Manager Instance;

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

        Instantiate(charPrefab, spawnPoint.position, Quaternion.identity);
    }
}
