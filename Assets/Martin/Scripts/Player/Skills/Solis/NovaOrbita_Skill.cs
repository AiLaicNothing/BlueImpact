using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

[CreateAssetMenu(menuName = "Player/Skills/Solis/Nova Orbita")]
public class NovaOrbita_Skill : Skill
{
    [Header("Prefab")]
    public GameObject orbPrefab;
    public HitData hitData;

    [Header("Orbit Settings")]
    public float radius = 3f;
    public float angularSpeed = 180f; // degrees per second
    public float duration = 5f;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        SpawnOrb(player, 0f);
        SpawnOrb(player, 180f);
    }
    void SpawnOrb(PlayerControl player, float startAngle)
    {
        float rad = startAngle * Mathf.Deg2Rad;

        Vector3 forward = player.Model.forward;
        Vector3 right = player.Model.right;

        Vector3 offset = forward * Mathf.Cos(rad) * radius + right * Mathf.Sin(rad) * radius;

        Vector3 spawnPos = player.transform.position + offset;

        GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);

        NovaOrbita_Orb orbScript = orb.GetComponent<NovaOrbita_Orb>();
        orbScript.Initialize(player, hitData, radius, angularSpeed, duration, startAngle);
    }
}
