using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character/Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character")]
    public string characterID;
    public string characterName;

    [Header("Base Stats")]
    public BaseStats baseStats;

    [Header("Stat points")]
    [Min(0)]
    public int startingStatPoints;
}
