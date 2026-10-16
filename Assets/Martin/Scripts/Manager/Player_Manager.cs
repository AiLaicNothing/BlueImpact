using UnityEngine;

public class Player_Manager : MonoBehaviour
{
    //The purpose of this is to be a Instance accesible to get the player objet or relate info like stats

    public static Player_Manager Instance;

    private GameObject player;

    private CharacterInfo data;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetPlayer(GameObject playerObject)
    {
        player = playerObject;
    }

    public GameObject GetPlayer()
    {
        return player;
    }

    public void SetCharacterData(CharacterInfo characterData)
    {
        data = characterData;
    }

    public CharacterInfo GetCharacterData()
    {
        return data;
    }
}
