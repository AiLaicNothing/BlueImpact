using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Aria Aqua")]
public class AriaAqua_Skill : Skill
{
    [Header("Prefabs")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject centerVFX;
    [SerializeField] private GameObject explosionVFX;

    [Header("Targeting")]
    [SerializeField] private float maxRange = 25f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Spawn")]
    [SerializeField] private float spawnDistance = 10f;
    [SerializeField] private float airHeight = 1f;

    [Header("Timing")]
    [SerializeField] private float travelTime = 1.5f;
    [SerializeField] private float explosionDelay = 0.2f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private HitData hitData;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.StartCoroutine(ExecuteRoutine(player, targetPoint, lockTargetPos));
    }

    private IEnumerator ExecuteRoutine(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;

        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        Vector3 groundCenter = finalTarget;

        // Snap to ground
        if (Physics.Raycast(groundCenter + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            groundCenter = hit.point;
        }

        Vector3 airCenter = groundCenter + Vector3.up * airHeight;

        if (centerVFX != null)
        {
            Instantiate(centerVFX, groundCenter, Quaternion.identity);
        }

        int arrivedCount = 0;

        void OnNodeArrive()
        {
            arrivedCount++;
        }

        Transform lockTarget = null;

        if (player.LockOnTarget != null && player.LockOnTarget.isTargeting && player.LockOnTarget.CurrentTarget != null)
        {
            lockTarget = player.LockOnTarget.CurrentTarget;
        }

        SpawnNodes(player, airCenter, lockTarget, OnNodeArrive);

        // Wait until all projectiles arrive
        yield return new WaitUntil(() => arrivedCount >= 3);

        yield return new WaitForSeconds(explosionDelay);

        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, airCenter, Quaternion.identity);
        }

        Explode(player, groundCenter);
    }

    void SpawnNodes(PlayerControl player, Vector3 center, Transform lockTarget, System.Action onArrive)
    {
        Vector3 forward = player.Model.forward;
        Vector3 right = player.Model.right;

        Vector3 p1 = center + forward * spawnDistance;
        Vector3 p2 = center - forward * spawnDistance + right * spawnDistance;
        Vector3 p3 = center - forward * spawnDistance - right * spawnDistance;

        CreateNode(p1, center, lockTarget, onArrive);
        CreateNode(p2, center, lockTarget, onArrive);
        CreateNode(p3, center, lockTarget, onArrive);
    }

    void CreateNode(Vector3 pos, Vector3 center, Transform lockTarget, System.Action onArrive)
    {
        GameObject obj = Instantiate(projectilePrefab, pos, Quaternion.identity);

        var node = obj.GetComponent<AriaAqua_Orb>();

        node.Initialize(center, lockTarget, travelTime, onArrive);
    }

    void Explode(PlayerControl player, Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius, enemyLayer);

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - center).normalized;

                DamageInfo info = new DamageInfo
                {
                    damage = ((player.Stats.GetCurrentValue(StatType.PhysicalDamage) * hitData.physicalScale) + (player.Stats.GetCurrentValue(StatType.MagicalDamage) * hitData.magicalScale)),
                    hitDirection = dir,
                    throwType = hitData.throwType,
                    stunDuration = hitData.stunDuration,
                    keepInAir = hitData.keepInAir,
                    airLiftForce = hitData.airLiftForce,
                    pushForce = hitData.pushForce,
                    knockDownForce = hitData.knockDownForce,
                    knockDownForwardScale = hitData.knockDownForwardScale,
                    staggerBuild = hitData.staggerCharge
                };

                dmg.TakeDamage(info);
            }
        }
    }
}
