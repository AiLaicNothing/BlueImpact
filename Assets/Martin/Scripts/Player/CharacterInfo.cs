using UnityEngine;

[CreateAssetMenu(menuName = "Characters/Character Data")]
public class CharacterInfo : ScriptableObject
{
    public string characterName;

    [TextArea]
    public string description;
    public Sprite portrait;
    public GameObject prefab;

    [Header("Stats value")]
    public int hp;
    public int stamina;
    public int mana;
    public int physicalDamage;
    public int magicalDamage;
}
