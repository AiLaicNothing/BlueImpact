using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Magic Beam")]
public class MagicBeam_Skill : Skill
{
    [Header("Beam Size")]
    [SerializeField] private float maxRange = 15f;
    [SerializeField] private float width = 2f;
    [SerializeField] private float height = 2f;
    [SerializeField] private bool debug;

    [Header("Time")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private float tickRate;

    [Header("Offset")]
    [SerializeField] private Vector3 startOffset;

    [Header("Damage")]
    [SerializeField] private HitData hitData;

    [Header("Layer")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask enemyLayer;
    private GameObject debugBox;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.StartCoroutine(BeamRoutine(player, targetPoint, lockTargetPos));
    }

    private IEnumerator BeamRoutine(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;

        float timer = 0f;

        while (timer < duration)
        {
            FireBeam(player, targetPoint, lockTargetPos);

            yield return new WaitForSeconds(tickRate);

            timer += tickRate;
        }

        if (debugBox != null)
        {
            GameObject.Destroy(debugBox);
            debugBox = null;
        }
    }

    private void FireBeam(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 startPos = player.transform.position + player.Model.right * startOffset.x + player.Model.up * startOffset.y + player.Model.forward * startOffset.z;

        Vector3 finalTarget;

        // PRIORITIZE LOCK TARGET
        if (lockTargetPos != Vector3.zero)
        {
            finalTarget = lockTargetPos;
        }
        else
        {
            finalTarget = targetPoint;
        }

        // Direction toward target
        Vector3 dir = (finalTarget - startPos).normalized;

        float finalDistance = maxRange;

        // Obstacle check
        if (Physics.Raycast(startPos, dir, out RaycastHit hit, maxRange, obstacleLayer))
        {
            finalDistance = hit.distance;
        }

        Vector3 center = startPos + dir * (finalDistance / 2f);

        Vector3 halfExtents = new Vector3(width / 2f, height / 2f, finalDistance / 2f);

        Quaternion rot = Quaternion.LookRotation(dir);

        if (debug) debugBox = player.ShowHitboxPersistent(center, halfExtents * 2, rot, debugBox);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, enemyLayer);

        DamageInfo info = new DamageInfo
        {
            damage = 1,
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

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(info);
            }
        }
    }
}
