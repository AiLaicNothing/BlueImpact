using UnityEngine;

public abstract class Skill : ScriptableObject
{
    [Header("Info")]
    public string skillId;
    public Sprite skillSprite;

    public string skillName;

    //public CharacterType ownerCharacter;

    [Header("Cost")]
    public StatType resourceType;
    public int cost;

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

    public abstract void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos);

    // ==================== NUEVOS: DAÑO Y ESCALADO ====================

    /// <summary>
    /// Retorna el daño base de la skill (override en subclases)
    /// </summary>
    public virtual int GetBaseDamage() => 0;

    /// <summary>
    /// Retorna el escalado de daño físico en formato string (ej: "120%")
    /// </summary>
    public virtual string GetPhysicalScaling() => "";

    /// <summary>
    /// Retorna el escalado de daño mágico en formato string (ej: "90%")
    /// </summary>
    public virtual string GetMagicScaling() => "";

    /// <summary>
    /// Retorna la descripción detallada del daño (override para skills complejas)
    /// </summary>
    public virtual string GetDamageDescription()
    {
        int baseDmg = GetBaseDamage();
        string physicalScaling = GetPhysicalScaling();
        string magicScaling = GetMagicScaling();

        if (baseDmg == 0 && string.IsNullOrEmpty(physicalScaling) && string.IsNullOrEmpty(magicScaling))
            return ""; // No tiene daño

        string description = "";

        // Daño base
        if (baseDmg > 0)
            description += $"<b>Daño:</b> {baseDmg}\n";

        // Escalados
        if (!string.IsNullOrEmpty(physicalScaling))
            description += $"<b>Escalado Físico:</b> {physicalScaling}\n";

        if (!string.IsNullOrEmpty(magicScaling))
            description += $"<b>Escalado Mágico:</b> {magicScaling}\n";

        return description;
    }
}