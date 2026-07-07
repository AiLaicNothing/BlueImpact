using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/Super Nova")]
public class SuperNova_Skill : Skill
{
    [Header("Prefab")]
    public GameObject orbPrefab;
    public HitData hitData;
    public HitData explosionData;

    [Header("Orbit Settings")]
    public float radius = 3f;
    public float angularSpeed = 180f; // degrees per second
    public float duration = 5f;

    [Header("Explosion")]
    public float explosionRadius = 5f;
    public LayerMask enemyLayer;

    [Header("Vfx")]
    [SerializeField] private GameObject explosionVfx;

    // ==================== NUEVOS: DAÑO Y ESCALADO ====================
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";

    /// <summary>
    /// Override para mostrar información de daño + explosión
    /// </summary>
    public override string GetDamageDescription()
    {
        string description = "";

        // Daño orbital
        if (!string.IsNullOrEmpty(GetPhysicalScaling()) || !string.IsNullOrEmpty(GetMagicScaling()))
        {
            description += "<b>Daño Orbital:</b>\n";
            if (!string.IsNullOrEmpty(GetPhysicalScaling()))
                description += $"  <b>Escalado Físico:</b> {GetPhysicalScaling()}\n";
            if (!string.IsNullOrEmpty(GetMagicScaling()))
                description += $"  <b>Escalado Mágico:</b> {GetMagicScaling()}\n";
        }

        // Daño explosión
        if (explosionData != null)
        {
            description += "\n<b>Daño Explosión:</b>\n";
            description += $"  <b>Escalado Físico:</b> {explosionData.physicalScale * 100:F0}%\n";
            description += $"  <b>Escalado Mágico:</b> {explosionData.magicalScale * 100:F0}%\n";
        }

        return description;
    }

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        SpawnOrb(player, 0f);
        SpawnOrb(player, 180f);
        player.PlayAudio(actionSound, 0.8f); // ✅ una vez después de spawnear ambos orbs

    }
    void SpawnOrb(PlayerControl player, float startAngle)
    {
        float rad = startAngle * Mathf.Deg2Rad;

        Vector3 forward = player.Model.forward;
        Vector3 right = player.Model.right;

        Vector3 offset = forward * Mathf.Cos(rad) * radius + right * Mathf.Sin(rad) * radius;

        Vector3 spawnPos = player.transform.position + offset;

        GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);

        SuperNova_Orb orbScript = orb.GetComponent<SuperNova_Orb>();
        orbScript.Initialize(player, hitData, radius, angularSpeed, duration, startAngle, explosionRadius, explosionData, enemyLayer, explosionVfx);
    }
}