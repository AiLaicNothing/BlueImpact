using UnityEngine;

public class Skill : ScriptableObject
{
    [Header("Info")]
    public string skillId;
    public Sprite skillSprite;

    public string skillName;

    //public CharacterType ownerCharacter;

    [Header("Cost")]
    public ResourceType resourceType;
    public float cost;

    [Header("Casting")]
    public float castTime;
    public float cooldown;
    public float actionTime;

    [Header("Animation")]
    public string castAnimation;
    public string actionAnimation;

    [Header("Audio")]
    public AudioClip castSound;
    public AudioClip actionSound;

    //public abstract void LocalExecute(PlayerController player, Vector3 targetPoint);

    //public abstract void ServerExecute(
    //    PlayerController player,
    //    Vector3 targetPoint,
    //    Vector3 lockTargetPos
    //);
}