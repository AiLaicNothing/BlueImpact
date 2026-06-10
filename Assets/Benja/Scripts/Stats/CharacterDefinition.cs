using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Characters/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Header("Character")]
    public string characterName;

    public Sprite characterIcon;

    [Header("Base Stats")]
    public List<CharacterStatValue> baseStats = new();
}